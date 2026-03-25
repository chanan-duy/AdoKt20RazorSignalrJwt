window.chatPage = (() => {
    const storageKey = "chatAuth";

    let auth = loadAuth();
    let connection = null;
    let elements = null;
    let options = null;

    function loadAuth() {
        try {
            const raw = localStorage.getItem(storageKey);
            return raw ? JSON.parse(raw) : null;
        } catch {
            return null;
        }
    }

    function saveAuth(session) {
        auth = session;
        localStorage.setItem(storageKey, JSON.stringify(session));
    }

    function clearAuth() {
        auth = null;
        localStorage.removeItem(storageKey);
    }

    function cacheElements() {
        elements = {
            authView: document.getElementById("authView"),
            chatView: document.getElementById("chatView"),
            loginForm: document.getElementById("loginForm"),
            loginButton: document.getElementById("loginButton"),
            loginError: document.getElementById("loginError"),
            usernameInput: document.getElementById("usernameInput"),
            passwordInput: document.getElementById("passwordInput"),
            currentUser: document.getElementById("currentUser"),
            logoutButton: document.getElementById("logoutButton"),
            messageInput: document.getElementById("messageInput"),
            sendButton: document.getElementById("sendButton"),
            chatStatus: document.getElementById("chatStatus"),
            messagesList: document.getElementById("messagesList")
        };
    }

    function createMessageElement(message) {
        const wrapper = document.createElement("div");
        wrapper.className = "message-item";

        const meta = document.createElement("div");
        meta.className = "message-meta";

        const user = document.createElement("strong");
        user.textContent = message.username;

        const time = document.createElement("span");
        time.textContent = new Date(message.createdAtUtc).toLocaleString();

        meta.appendChild(user);
        meta.appendChild(time);

        const text = document.createElement("p");
        text.className = "message-text";
        text.textContent = message.text;

        wrapper.appendChild(meta);
        wrapper.appendChild(text);

        return wrapper;
    }

    function renderMessages(messages) {
        elements.messagesList.replaceChildren();

        if (!messages.length) {
            const empty = document.createElement("div");
            empty.id = "emptyState";
            empty.className = "alert alert-light border mb-0";
            empty.textContent = "nothing yet";
            elements.messagesList.appendChild(empty);
            return;
        }

        messages.forEach(message => {
            elements.messagesList.appendChild(createMessageElement(message));
        });

        elements.messagesList.scrollTop = elements.messagesList.scrollHeight;
    }

    function appendMessage(message) {
        const emptyState = document.getElementById("emptyState");

        if (emptyState) {
            emptyState.remove();
        }

        elements.messagesList.appendChild(createMessageElement(message));
        elements.messagesList.scrollTop = elements.messagesList.scrollHeight;
    }

    function setStatus(text, isError) {
        elements.chatStatus.textContent = text;
        elements.chatStatus.classList.toggle("text-danger", !!isError);
        elements.chatStatus.classList.toggle("text-secondary", !isError);
    }

    function showLoginError(text) {
        if (!text) {
            elements.loginError.textContent = "";
            elements.loginError.classList.add("d-none");
            return;
        }

        elements.loginError.textContent = text;
        elements.loginError.classList.remove("d-none");
    }

    function showLoggedOutView() {
        elements.authView.classList.remove("d-none");
        elements.chatView.classList.add("d-none");
        showLoginError("");
        setStatus("signed out", false);
    }

    function showChatView() {
        elements.currentUser.textContent = auth.username;
        elements.authView.classList.add("d-none");
        elements.chatView.classList.remove("d-none");
    }

    function getAuthHeaders() {
        return {
            Authorization: `Bearer ${auth.token}`
        };
    }

    async function stopConnection() {
        if (!connection) {
            return;
        }

        const activeConnection = connection;
        connection = null;
        await activeConnection.stop();
    }

    async function logout() {
        await stopConnection();
        clearAuth();
        renderMessages([]);
        elements.passwordInput.value = "";
        showLoggedOutView();
    }

    async function loadMessages() {
        const response = await fetch(options.messagesUrl, {
            headers: getAuthHeaders()
        });

        if (response.status === 401) {
            return false;
        }

        if (!response.ok) {
            throw new Error("could not load messages");
        }

        const messages = await response.json();
        renderMessages(messages);
        return true;
    }

    async function startConnection() {
        await stopConnection();

        connection = new signalR.HubConnectionBuilder()
            .withUrl(options.hubUrl, {
                accessTokenFactory: () => auth?.token ?? ""
            })
            .withAutomaticReconnect()
            .build();

        connection.on("ReceiveMessage", message => {
            appendMessage(message);
        });

        connection.onreconnecting(() => {
            setStatus("reconnecting...", true);
            elements.sendButton.disabled = true;
        });

        connection.onreconnected(() => {
            setStatus(`connected as ${auth.username}`, false);
            elements.sendButton.disabled = false;
        });

        connection.onclose(() => {
            setStatus("disconnected", true);
            elements.sendButton.disabled = true;
        });

        await connection.start();
        setStatus(`connected as ${auth.username}`, false);
        elements.sendButton.disabled = false;
        elements.messageInput.focus();
    }

    async function activateChat() {
        showChatView();
        elements.sendButton.disabled = true;

        const isAuthorized = await loadMessages();
        if (!isAuthorized) {
            await logout();
            showLoginError("session expired");
            return;
        }

        try {
            await startConnection();
        } catch {
            setStatus("could not connect", true);
        }
    }

    async function login(event) {
        event.preventDefault();

        showLoginError("");
        elements.loginButton.disabled = true;

        try {
            const response = await fetch(options.loginUrl, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    username: elements.usernameInput.value,
                    password: elements.passwordInput.value
                })
            });

            if (response.status === 401) {
                showLoginError("invalid user or password");
                return;
            }

            if (response.status === 400) {
                const payload = await response.json();
                const errors = Object.values(payload.errors ?? {}).flat();
                showLoginError(errors[0] ?? "invalid login");
                return;
            }

            if (!response.ok) {
                showLoginError("login failed");
                return;
            }

            const session = await response.json();
            saveAuth(session);
            elements.passwordInput.value = "";
            await activateChat();
        } catch {
            showLoginError("login failed");
        } finally {
            elements.loginButton.disabled = false;
        }
    }

    async function sendMessage() {
        const text = elements.messageInput.value.trim();

        if (!text) {
            setStatus("write a message", true);
            return;
        }

        if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
            setStatus("not connected", true);
            return;
        }

        elements.sendButton.disabled = true;

        try {
            await connection.invoke("SendMessage", text);
            elements.messageInput.value = "";
            elements.messageInput.focus();
            setStatus(`connected as ${auth.username}`, false);
        } catch {
            setStatus("could not send", true);
        } finally {
            if (connection && connection.state === signalR.HubConnectionState.Connected) {
                elements.sendButton.disabled = false;
            }
        }
    }

    async function init(initOptions) {
        options = initOptions;
        cacheElements();

        elements.loginForm.addEventListener("submit", login);
        elements.logoutButton.addEventListener("click", logout);
        elements.sendButton.addEventListener("click", sendMessage);
        elements.messageInput.addEventListener("keydown", async event => {
            if (event.key === "Enter" && !event.shiftKey) {
                event.preventDefault();
                await sendMessage();
            }
        });

        showLoggedOutView();

        if (auth?.token && auth?.username) {
            await activateChat();
        }
    }

    return {
        init
    };
})();
