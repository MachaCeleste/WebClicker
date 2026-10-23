import { getValidAccessToken } from './auth.js?v=0.1.0';
import { ServerMessage, ClientMessage } from './network.js?v=0.1.0';
import { Upgrade } from './classes.js?v=0.1.0';

let ownedUpgrades = [];
let currentTheme = 0;
let currentClicks = 0;
let socket = null;

const themeMap = {
    0: 'default',
    1: 'default-dark',
    2: 'punk',
    3: 'forest',
    4: 'cyberpunk',
    5: 'sunset',
    6: 'abyss',
}

const upgradeMap = {
    1: 100,
    2: 100,
    3: 250,
}

const counterDisplay = document.getElementById('counterDisplay');
const leaderboardText = document.getElementById('leaderboardText');

const leaderboardPanel = document.getElementById('leaderboardPanel');
const upgradePanel = document.getElementById('upgradePanel');
const themePanel = document.getElementById('themePanel');
const chatPanel = document.getElementById('chatPanel');

window.closeAllPanels = () => {
    document.querySelectorAll('.side-panel').forEach(panel => { panel.classList.remove('open') });
}
window.purchaseUpgrade = (id, data = "") => {
    const msg = new ServerMessage(2);
    msg.addInt(id);
    if (data) msg.addString(data);
    msg.send(socket);
};
window.toggleUpgrade = (id) => {
    const msg = new ServerMessage(3);
    msg.addInt(id);
    msg.send(socket);
};
window.showNickInput = () => {
    document.getElementById('nicknameUpgradeUI').style.display = 'none';
    document.getElementById('nicknameInputUI').style.display = 'flex';
};
window.cancelNickPurchase = () => {
    document.getElementById('nicknameUpgradeUI').style.display = 'flex';
    document.getElementById('nicknameInputUI').style.display = 'none';
};
window.finalizeNickPurcase = () => {
    const val = document.getElementById('nickInput').value.trim();
    if (/^[a-zA-Z0-9]{2,16}$/.test(val)) {
        window.purchaseUpgrade(1, val);
        cancelNickPurchase();
    } else alert("Invalid Nickname (2-16 Alphanumeric)");
};
window.sendChatMessage = () => {
    const chatInput = document.getElementById('chatInput');
    const content = chatInput.value;
    if (!content || content.trim().length === 0) return;
    chatInput.value = "";
    const msg = new ServerMessage(5);
    msg.addString(content);
    msg.send(socket);
}

document.getElementById('leaderboardBtn').onclick = () => {
    closeAllPanels();
    leaderboardPanel.classList.add('open');
}
document.getElementById('upgradeBtn').onclick = () => {
    closeAllPanels();
    upgradePanel.classList.add('open');
}
document.getElementById('themeBtn').onclick = () => {
    closeAllPanels();
    themePanel.classList.add('open');
}
document.getElementById('chatBtn').onclick = () => {
    closeAllPanels();
    chatPanel.classList.add('open');
}

document.getElementById('upgradeBtn').onclick = () => upgradePanel.classList.add('open');

async function initConnection() {
    const token = await getValidAccessToken();
    const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
    const wsUrl = `${protocol}//${window.location.host}/connect?token=${token}`;

    socket = new WebSocket(wsUrl);
    socket.onmessage = (e) => processMessageReceived(new ClientMessage(JSON.parse(e.data)));
    socket.onclose = (e) => setTimeout(initConnection, 3000);
}

function processMessageReceived(msg) {
    console.log(`Processing Message ID: ${msg.id}`);

    switch (msg.id) {
        case 1: alert(msg.getString()); break; // client message
        case 2: renderLeaderboard(msg); break; // update leaderboard
        case 3: // update counter
            currentClicks = msg.getInt();
            refreshGameUI();
            break;
        case 4: // update user info
            ownedUpgrades = JSON.parse(msg.getString()).map(u => new Upgrade(u));
            currentTheme = msg.getInt();
            refreshGameUI();
            break;
        case 5: renderChat(msg); break;
    }
}

function renderLeaderboard(msg) {
    leaderboardText.innerHTML = '';
    let rank = 1;
    while (msg.strValues.length > 0) {
        const [nick, userId, lifetime, clicks] = msg.getString().split('|');
        const entry = document.createElement('div');
        entry.className = 'leaderboard-entry';
        entry.setAttribute('title', `User ID: ${userId}\nLifetime: ${lifetime}`);
        entry.innerHTML = `<span class="rank-num">${rank++}.</b> ${nick}</span><span class="click-count">${clicks}</span>`;
        leaderboardText.appendChild(entry);
    }
}

function renderChat(msg) {
    const messageList = document.getElementById('messageList');
    const messagePrefab = document.querySelector('.chat-item');
    document.querySelectorAll('.chat-item').forEach(e => { if (window.getComputedStyle(e).display === 'flex') { e.remove(); }});
    const n = msg.strValues.length;
    for (let i = 0; i < n; i++) {
        const chat = msg.getString().split('|');
        const chatRow = messagePrefab.cloneNode(true);
        chatRow.querySelector('.item-name').innerText = chat[0];
        chatRow.setAttribute('title', `UserID: ${chat[1]}`)
        chatRow.querySelector('.item-cost').innerText = chat[2];
        chatRow.querySelector('.message-content').innerText = chat[3];
        chatRow.style.display = 'flex';
        messageList.appendChild(chatRow);
    }
    messageList.scrollTo({
        top: messageList.scrollHeight,
        behavior: 'smooth'
    });
}

function refreshGameUI() {
    counterDisplay.innerText = currentClicks;

    // Dynamic Upgrade
    document.querySelectorAll('.upgrade-item[data-upgrade-type]').forEach(row => {
        const type = parseInt(row.dataset.upgradeType);
        const data = ownedUpgrades.find(u => u.type === type);

        const level = data?.level || 0;
        const cost = data?.cost || upgradeMap[type];
        const active = data?.state === true;

        if (level > 0 || currentClicks >= cost) { // TODO add level cap handling
            row.style.display = 'flex';
            row.querySelector('.item-level').innerText = level > 0 ? `(Lv ${level})` : "";
            row.querySelector('.item-cost').innerText = `Cost: ${cost}`;

            const btn = row.querySelector('.item-buy-btn');
            btn.innerText = level > 0 ? "Upgrade" : "Buy";
            btn.onclick = () => window.purchaseUpgrade(type);
            btn.disabled = currentClicks < cost;

            const toggleWrap = row.querySelector('.toggle-container');
            if (level > 0 && data?.toggle) {
                toggleWrap.style.display = 'inline-block';
                const cb = row.querySelector('.item-toggle');
                cb.checked = active;
                cb.onclick = (e) => { e.stopPropagation(); window.toggleUpgrade(type); };
            } else {
                toggleWrap.style.display = 'none';
            }
        } else {
            row.style.display = 'none';
        }
    });

    // Nickname
    const hasNick = ownedUpgrades.find(u => u.type === 1);
    document.getElementById('nicknameUpgradeUI').style.display = !hasNick && currentClicks >= 100 ? 'flex' : 'none';

    // Theme
    const theme = ownedUpgrades.find(u => u.type === 3);
    document.getElementById('themeBtn').style.display = theme ? 'flex' : 'none';

    if (theme) {
        const themeList = document.getElementById('themeList');
        const themePrefab = themeList.querySelector('.theme-item');

        for (let i = 0; i < theme.level + 1; i++) {
            const themeName = themeMap[i];
            let themeRow = document.querySelector(`.theme-item[data-theme-number='${i}']`);
            if (!themeRow) {
                themeRow = themePrefab.cloneNode(true);
                themeRow.dataset.themeNumber = i;
                const properName = themeName.charAt(0).toUpperCase() + themeName.slice(1);
                themeRow.querySelector('.item-name').innerText = properName;
                themeList.appendChild(themeRow);
            }
            const checkbox = themeRow.querySelector('.item-toggle');
            const state = i === currentTheme;
            checkbox.checked = state;
            checkbox.disabled = state;
            if (state) {
                const themePath = `/themes/theme-${themeName}.css`;
                document.getElementById('theme-link').setAttribute('href', themePath);
            }
            checkbox.onclick = (e) => {
                if (checkbox.checked) {
                    const msg = new ServerMessage(4);
                    msg.addInt(i);
                    msg.send(socket);
                }
            }
        }
    }
}

document.getElementById('clickBtn').onclick = () => {
    if (socket?.readyState === 1) new ServerMessage(1).send(socket);
};

initConnection();
