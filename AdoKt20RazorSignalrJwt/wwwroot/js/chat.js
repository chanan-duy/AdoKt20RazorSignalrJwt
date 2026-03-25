window.chatPage = (() => {
    let connection;

    function createMessageElement(message) {
        const wrapper = document.createElement("div");
        wrapper.className = "message-item";

        const meta = document.createElement("div");
        meta.className = "message-meta";

        const user = document.createElement("strong");
        user.textContent = message.user;

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

    function appendMessage(message) {
        const messagesList = document.getElementById("messagesList");
        const emptyState = document.getElementById("emptyState");

        if (emptyState) {
            emptyState.remove();
        }

        messagesList.appendChild(createMessageElement(message));
        messagesList.scrollTop = messagesList.scrollHeight;
    }

    function setStatus(text, isError) {
        const status = document.getElementById("chatStatus");
        status.textContent = text;
        status.classList.toggle("text-danger", !!isError);
        status.classList.toggle("text-secondary", !isError);
    }

    async function sendMessage() {
        const input = document.getElementById("messageInput");
        const button = document.getElementById("sendButton");
        const text = input.value.trim();

        if (!text) {
            setStatus("write a message", true);
            return;
        }

        button.disabled = true;

        try {
            await connection.invoke("SendMessage", text);
            input.value = "";
            input.focus();
            setStatus("connected", false);
        } catch {
            setStatus("could not send", true);
        } finally {
            button.disabled = false;
        }
    }

    async function init(options) {
        const input = document.getElementById("messageInput");
        const button = document.getElementById("sendButton");
        const messagesList = document.getElementById("messagesList");

        messagesList.scrollTop = messagesList.scrollHeight;

        connection = new signalR.HubConnectionBuilder()
            .withUrl(options.hubUrl)
            .withAutomaticReconnect()
            .build();

        connection.on("ReceiveMessage", message => {
            appendMessage(message);
        });

        connection.onreconnecting(() => {
            setStatus("reconnecting...", true);
            button.disabled = true;
        });

        connection.onreconnected(() => {
            setStatus("connected", false);
            button.disabled = false;
        });

        connection.onclose(() => {
            setStatus("disconnected", true);
            button.disabled = true;
        });

        button.addEventListener("click", sendMessage);
        input.addEventListener("keydown", async event => {
            if (event.key === "Enter" && !event.shiftKey) {
                event.preventDefault();
                await sendMessage();
            }
        });

        try {
            await connection.start();
            setStatus(`connected as ${options.currentUser}`, false);
            input.focus();
        } catch {
            setStatus("could not connect", true);
            button.disabled = true;
        }
    }

    return {
        init
    };
})();
