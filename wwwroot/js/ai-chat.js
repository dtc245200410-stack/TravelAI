(() => {
    'use strict';
    const config = JSON.parse(document.getElementById('chatConfig').textContent);
    const app = document.getElementById('chatApp');
    const messages = document.getElementById('messages');
    const input = document.getElementById('messageInput');
    const sendButton = document.getElementById('sendButton');
    const stopButton = document.getElementById('stopButton');
    const feedback = document.getElementById('feedback');
    const list = document.getElementById('conversationList');
    const title = document.getElementById('currentTitle');
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    const base = config.baseUrl.replace(/\/$/, '');
    let conversationId = config.conversationId;
    let conversations = config.conversations || [];
    let active = null;
    let latestAssistant = null;
    let lastQuestion = '';
    const endpoint = action => base + '/' + action;
    const create = (tag, cls, text) => {
        const node = document.createElement(tag);
        if (cls) node.className = cls;
        if (text !== undefined) node.textContent = text;
        return node;
    };
    function notify(text, error = false) { feedback.textContent = text; feedback.classList.toggle('error', error); }
    function allowLink(href) {
        try {
            const url = new URL(href, location.origin);
            return url.origin === location.origin && /^\/(Tour|Hotel|AIConversationAdmin|Admin(?:Tour|Hotel|Booking|Payment|Promotion|Review|User|Room|Combo|Statistics)?)(?:\/|\?|$)/.test(url.pathname);
        } catch { return false; }
    }
    // DOM-only Markdown: model output never enters innerHTML or an executable HTML parser.
    function inline(parent, text) {
        const pattern = /\\([\\*\[\]|])|\[([^\]]+)\]\(([^\s)]+)\)|\*\*([^*]+)\*\*|`([^`]+)`|_([^_]+)_/g;
        let last = 0;
        for (const match of text.matchAll(pattern)) {
            parent.append(document.createTextNode(text.slice(last, match.index)));
            if (match[1]) parent.append(document.createTextNode(match[1]));
            else if (match[2]) {
                if (allowLink(match[3])) { const link = create('a', '', match[2]); link.href = match[3]; parent.append(link); }
                else parent.append(document.createTextNode(match[2]));
            } else if (match[4]) parent.append(create('strong', '', match[4]));
            else if (match[5]) parent.append(create('code', '', match[5]));
            else if (match[6]) parent.append(create('em', '', match[6]));
            last = match.index + match[0].length;
        }
        parent.append(document.createTextNode(text.slice(last)));
    }
    const cells = line => line.trim().replace(/^\||\|$/g, '').split(/(?<!\\)\|/).map(x => x.trim());
    function markdown(node, source) {
        node.replaceChildren();
        const lines = source.replace(/\r/g, '').split('\n');
        let i = 0;
        while (i < lines.length) {
            const line = lines[i];
            if (!line.trim()) { i++; continue; }
            if (line.startsWith('```')) {
                const code = []; i++;
                while (i < lines.length && !lines[i].startsWith('```')) code.push(lines[i++]);
                const pre = create('pre'); pre.append(create('code', '', code.join('\n'))); node.append(pre); i++; continue;
            }
            if (line.trim().startsWith('|') && i + 1 < lines.length && /^\s*\|?\s*:?-{3,}/.test(lines[i + 1])) {
                const table = create('table'); const head = create('thead'); const row = create('tr');
                for (const cell of cells(line)) { const th = create('th'); inline(th, cell); row.append(th); }
                head.append(row); table.append(head); i += 2;
                const body = create('tbody');
                while (i < lines.length && lines[i].trim().startsWith('|')) {
                    const tr = create('tr'); for (const cell of cells(lines[i++])) { const td = create('td'); inline(td, cell); tr.append(td); } body.append(tr);
                }
                table.append(body); const wrapper = create('div', 'table-wrap'); wrapper.append(table); node.append(wrapper); continue;
            }
            if (/^#{1,3}\s/.test(line)) { const h = create('h3'); inline(h, line.replace(/^#{1,3}\s/, '')); node.append(h); i++; continue; }
            if (/^\s*(- |\d+\. )/.test(line)) {
                const ol = /^\s*\d+\./.test(line); const ul = create(ol ? 'ol' : 'ul');
                while (i < lines.length && /^\s*(- |\d+\. )/.test(lines[i])) {
                    const li = create('li'); inline(li, lines[i++].replace(/^\s*(- |\d+\. )/, '')); ul.append(li);
                }
                node.append(ul); continue;
            }
            const p = create('p'); inline(p, line); node.append(p); i++;
        }
    }
    function nearBottom() { return messages.scrollHeight - messages.scrollTop - messages.clientHeight < 110; }
    function scroll(force = false) { if (force || nearBottom()) messages.scrollTop = messages.scrollHeight; }
    function addMessage(role, content, id = null) {
        const row = create('article', 'message ' + (role === 'User' ? 'user' : 'assistant'));
        const body = create('div', 'message-body');
        const result = { row, content, id, role };
        if (role === 'User') body.textContent = content;
        else {
            row.append(create('div', 'assistant-avatar', '✦'));
            const render = create('div', 'markdown'); body.append(render); result.render = render;
            markdown(render, content);
            const actions = create('div', 'message-actions');
            const copy = create('button', '', 'Sao chép'); copy.type = 'button';
            copy.addEventListener('click', async () => {
                try { await navigator.clipboard.writeText(result.content); notify('Đã sao chép câu trả lời.'); }
                catch { notify('Không sao chép tự động được. Bạn có thể chọn văn bản và nhấn Ctrl+C.', true); }
            });
            const regenerate = create('button', '', '↻ Tạo lại'); regenerate.type = 'button';
            regenerate.addEventListener('click', () => { if (latestAssistant === result && !active && lastQuestion) send(lastQuestion, true); });
            actions.append(copy, regenerate); body.append(actions); result.regenerate = regenerate;
            if (latestAssistant?.regenerate) latestAssistant.regenerate.hidden = true;
            latestAssistant = result;
        }
        row.append(body); messages.append(row); scroll(true); return result;
    }
    function welcome() {
        messages.replaceChildren(); latestAssistant = null; lastQuestion = '';
        const box = create('section', 'welcome'); box.id = 'welcome';
        box.append(create('div', 'welcome-symbol', '✦'));
        box.append(create('h1', '', config.isAdmin ? 'Hôm nay, hệ thống của bạn thế nào?' : 'Hành trình tiếp theo của bạn là gì?'));
        box.append(create('p', '', config.isAdmin ? 'Tra cứu số liệu, tổng hợp báo cáo và tìm việc cần ưu tiên. Mỗi gợi ý dẫn đến đúng màn hình quản trị.' : 'Tìm tour phù hợp, so sánh giá và lên kế hoạch cùng TravelAI. Thông tin được lấy từ sản phẩm thực tế trong hệ thống.'));
        const suggestions = create('div', 'suggestions');
        config.suggestions.forEach((text, i) => {
            const button = create('button', 'suggestion'); button.type = 'button';
            button.append(create('span', '', ['⌁', '◈', '↗', '✧'][i]), document.createTextNode(text));
            button.addEventListener('click', () => send(text)); suggestions.append(button);
        });
        box.append(suggestions); messages.append(box);
    }
    function displayHistory() {
        list.replaceChildren();
        const term = document.getElementById('historySearch').value.toLocaleLowerCase('vi');
        const filtered = conversations.filter(c => (c.title || '').toLocaleLowerCase('vi').includes(term));
        if (!filtered.length) list.append(create('div', 'empty-history', term ? 'Không có hội thoại phù hợp.' : 'Những cuộc trò chuyện của bạn sẽ xuất hiện ở đây.'));
        filtered.forEach(c => {
            const row = create('div', 'history-row' + (c.id === conversationId ? ' active' : ''));
            const button = create('button', 'history-select', c.title || 'Cuộc trò chuyện'); button.type = 'button'; button.title = c.title || '';
            button.addEventListener('click', () => select(c.id));
            const actions = create('div', 'history-actions');
            const rename = create('button', '', '✎'); rename.type = 'button'; rename.setAttribute('aria-label', 'Đổi tên hội thoại');
            rename.addEventListener('click', async () => {
                if (active) return notify('Hãy dừng hoặc chờ trả lời xong.');
                const name = prompt('Tên cuộc trò chuyện:', c.title || ''); if (name === null || !name.trim()) return;
                try { await post('Rename', { conversationId: c.id, title: name }); c.title = name.trim(); displayHistory(); if (c.id === conversationId) title.textContent = c.title; }
                catch (error) { notify(error.message, true); }
            });
            const remove = create('button', '', '×'); remove.type = 'button'; remove.setAttribute('aria-label', 'Xóa hội thoại');
            remove.addEventListener('click', async () => {
                if (active) return notify('Hãy dừng hoặc chờ trả lời xong.');
                if (!confirm('Xóa cuộc trò chuyện này cùng các tin nhắn?')) return;
                try { await post('Delete', { conversationId: c.id }); conversations = conversations.filter(x => x.id !== c.id); if (conversationId === c.id) newChat(); else displayHistory(); }
                catch (error) { notify(error.message, true); }
            });
            actions.append(rename, remove); row.append(button, actions); list.append(row);
        });
    }
    async function checkResponse(response) {
        if (response.ok) return;
        let error; try { error = await response.json(); } catch { /* HTML antiforgery errors are intentionally not rendered. */ }
        const message = typeof error?.message === 'string' ? error.message : null;
        if (response.status === 401) throw new Error(message || 'Phiên đăng nhập đã hết. Hãy đăng xuất rồi đăng nhập lại.');
        if (response.status === 403) throw new Error(message || 'Tài khoản không có quyền truy cập trang này. Chat quản lí yêu cầu tài khoản Admin.');
        if (response.status === 400) throw new Error(message || 'Phiên xác thực biểu mẫu không hợp lệ. Hãy tải lại trang rồi gửi lại.');
        throw new Error(message || 'Yêu cầu thất bại. Hãy tải lại trang hoặc thử lại.');
    }
    async function post(action, values) {
        const body = new URLSearchParams({ ...values, __RequestVerificationToken: token });
        const response = await fetch(endpoint(action), { method: 'POST', headers: { 'X-Requested-With': 'XMLHttpRequest' }, body });
        await checkResponse(response); return response.json();
    }
    async function refreshHistory() {
        const response = await fetch(endpoint('Conversations'), { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
        await checkResponse(response); conversations = (await response.json()).conversations; displayHistory();
    }
    async function select(id) {
        if (active) return notify('Hãy dừng hoặc chờ trả lời xong trước khi đổi hội thoại.');
        try {
            const response = await fetch(endpoint('GetMessages') + '?conversationId=' + id, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
            await checkResponse(response); const data = await response.json();
            conversationId = data.conversationId; title.textContent = data.title || 'Cuộc trò chuyện';
            messages.replaceChildren(); latestAssistant = null; lastQuestion = '';
            data.messages.forEach(m => { addMessage(m.role, m.content, m.id); if (m.role === 'User') lastQuestion = m.content; });
            if (!data.messages.length) welcome();
            displayHistory(); setUrl(); app.classList.remove('mobile-sidebar-open'); notify('');
        } catch (error) { notify(error.message, true); }
    }
    function setUrl() { history.replaceState(null, '', base + (conversationId ? '?conversationId=' + conversationId : '')); }
    function newChat() {
        if (active) return notify('Hãy dừng hoặc chờ trả lời xong.');
        conversationId = null; title.textContent = 'Cuộc trò chuyện mới'; input.value = ''; sizeInput();
        welcome(); displayHistory(); setUrl(); notify(''); app.classList.remove('mobile-sidebar-open'); input.focus();
    }
    function sizeInput() { input.style.height = 'auto'; input.style.height = Math.min(input.scrollHeight, 140) + 'px'; }
    async function send(question, regenerate = false) {
        if (active || !question.trim()) return;
        if (question.length > 2000) return notify('Câu hỏi tối đa 2.000 ký tự.', true);
        const oldAssistant = regenerate ? latestAssistant : null;
        const oldContent = oldAssistant?.content;
        document.getElementById('welcome')?.remove();
        if (!regenerate) { addMessage('User', question); lastQuestion = question; }
        if (oldAssistant) { oldAssistant.row.remove(); latestAssistant = null; }
        const answer = addMessage('Assistant', ''); answer.regenerate.hidden = true;
        const typing = create('div', 'typing'); typing.append(create('span'), create('span'), create('span')); answer.render.append(typing);
        input.value = ''; sizeInput(); sendButton.hidden = true; stopButton.hidden = false; input.disabled = true;
        const controller = new AbortController(); active = controller;
        let complete = false; let gotData = false; let renderQueued = false; const start = performance.now();
        const timer = setInterval(() => notify('Đang xử lý · ' + Math.floor((performance.now() - start) / 1000) + ' giây'), 1000);
        notify('Đang lấy dữ liệu hệ thống…');
        const render = () => {
            if (renderQueued) return;
            renderQueued = true; requestAnimationFrame(() => { const stick = nearBottom(); markdown(answer.render, answer.content); if (stick) scroll(true); renderQueued = false; });
        };
        function event(frame) {
            const lines = frame.split('\n'); const name = lines.find(x => x.startsWith('event: '))?.slice(7);
            const payload = lines.filter(x => x.startsWith('data: ')).map(x => x.slice(6)).join('\n');
            if (!payload) return;
            const data = JSON.parse(payload);
            if (name === 'meta') {
                conversationId = data.conversationId; title.textContent = data.title; setUrl();
                const existing = conversations.find(x => x.id === conversationId);
                if (!existing) conversations.unshift({ id: conversationId, title: data.title }); displayHistory();
            } else if (name === 'delta') { gotData = true; answer.content += data.text; render(); }
            else if (name === 'done') {
                complete = true;
                clearInterval(timer);
                const seconds = (data.elapsedMs / 1000).toFixed(1);
                notify(data.saved ? 'Hoàn tất · ' + seconds + ' giây' + (data.mode === 'ollama' ? '' : ' · Kết quả từ dữ liệu hệ thống') : 'Đã nhận trả lời nhưng chưa lưu được lịch sử.', !data.saved);
            }
        }
        try {
            const body = new URLSearchParams({ message: question, regenerate: String(regenerate), __RequestVerificationToken: token });
            if (conversationId) body.set('conversationId', conversationId);
            const response = await fetch(endpoint('Chat'), { method: 'POST', body, signal: controller.signal,
                headers: { 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'text/event-stream' } });
            await checkResponse(response);
            if (!response.headers.get('content-type')?.includes('text/event-stream')) throw new Error('Không nhận được luồng chat. Hãy tải lại trang.');
            const reader = response.body.getReader(); const decoder = new TextDecoder(); let buffer = '';
            for (;;) {
                const { value, done } = await reader.read(); if (done) break;
                buffer += decoder.decode(value, { stream: true }).replace(/\r\n/g, '\n');
                let end; while ((end = buffer.indexOf('\n\n')) >= 0) { const frame = buffer.slice(0, end); buffer = buffer.slice(end + 2); event(frame); }
            }
            if (!complete) throw new Error('Kết nối kết thúc trước khi lưu xong. Hãy mở lại hội thoại để kiểm tra.');
        } catch (error) {
            if (error.name === 'AbortError') {
                answer.content += '\n\n_Đã dừng tạo câu trả lời._'; markdown(answer.render, answer.content); notify('Đã dừng. Phần đã tạo sẽ được lưu vào lịch sử.');
            } else {
                notify(error.message, true);
                if (!gotData) {
                    answer.content = oldContent || error.message;
                    markdown(answer.render, answer.content);
                    if (!regenerate) { input.value = question; sizeInput(); }
                }
            }
        } finally {
            clearInterval(timer); active = null; sendButton.hidden = false; stopButton.hidden = true; input.disabled = false;
            answer.regenerate.hidden = !conversationId || (!gotData && !oldContent); input.focus();
            // The server may still be persisting a stopped stream; do not overwrite visible partial text.
            refreshHistory().catch(() => {});
        }
    }
    document.getElementById('chatForm').addEventListener('submit', e => { e.preventDefault(); send(input.value.trim()); });
    input.addEventListener('input', sizeInput);
    input.addEventListener('keydown', e => { if (e.key === 'Enter' && !e.shiftKey && !e.isComposing) { e.preventDefault(); send(input.value.trim()); } });
    stopButton.addEventListener('click', () => active?.abort());
    document.getElementById('newChat').addEventListener('click', newChat);
    document.getElementById('historySearch').addEventListener('input', displayHistory);
    document.getElementById('toggleSidebar').addEventListener('click', () => app.classList.toggle(innerWidth <= 640 ? 'mobile-sidebar-open' : 'sidebar-collapsed'));
    document.getElementById('sidebarOverlay').addEventListener('click', () => app.classList.remove('mobile-sidebar-open'));
    const scrollButton = document.getElementById('scrollBottom');
    messages.addEventListener('scroll', () => { scrollButton.hidden = nearBottom(); });
    scrollButton.addEventListener('click', () => scroll(true));
    if (config.messages?.length) {
        config.messages.forEach(m => { addMessage(m.role, m.content, m.id); if (m.role === 'User') lastQuestion = m.content; });
    } else welcome();
    displayHistory(); sizeInput();
    fetch(endpoint('Status'), { headers: { 'X-Requested-With': 'XMLHttpRequest' } }).then(async response => {
        await checkResponse(response); const status = await response.json(); const node = document.getElementById('connection');
        node.classList.add(status.available ? 'online' : 'offline'); node.replaceChildren(create('span'), document.createTextNode(status.available ? 'Ollama local · đã kết nối' : 'Chế độ tra cứu dữ liệu')); node.title = status.message;
    }).catch(() => { document.getElementById('connection').textContent = 'Chưa kiểm tra được kết nối'; });
})();
