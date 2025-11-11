// wwwroot/js/chatHub.js
(function (window) {
    const KEY = "__chatHub"; // dùng global __chatHub

    if (window[KEY]) {
        // Đã khởi tạo rồi thì thôi (tránh chạy 2 lần)
        return;
    }

    let connection = null;
    let startedPromise = null;

    function createConnection(hubUrl) {
        if (!window.signalR) {
            console.error("[chatHub] signalR chưa load");
            return null;
        }

        if (!connection) {
            connection = new signalR.HubConnectionBuilder()
                .withUrl(hubUrl)
                .withAutomaticReconnect()
                .build();

            // 👉 GÁN CONNECTION RA GLOBAL ĐỂ ChatCustomer XÀI
            window.__chatConnection = connection;
        }

        return connection;
    }

    function start(hubUrl, registerAccountId) {
        const conn = createConnection(hubUrl);
        if (!conn) return Promise.reject("No connection");

        if (!startedPromise || conn.state === "Disconnected") {
            startedPromise = conn
                .start()
                .then(async () => {
                    console.log("[chatHub] ✅ Connected to", hubUrl);
                    if (registerAccountId) {
                        try {
                            await conn.invoke("Register", registerAccountId);
                            console.log("[chatHub] ✅ Registered:", registerAccountId);
                        } catch (e) {
                            console.error("[chatHub] ❌ Register failed", e);
                        }
                    }
                })
                .catch(err => {
                    console.error("[chatHub] ❌ Connect failed", err);
                    // Retry nhẹ sau 5s
                    setTimeout(() => start(hubUrl, registerAccountId), 5000);
                    throw err;
                });
        }

        return startedPromise;
    }

    const api = {
        start,                       // window.__chatHub.start(hubUrl, accId)
        getConnection: () => connection,
        on: (eventName, handler) => {
            if (!connection) {
                console.warn("[chatHub] on() gọi trước khi start");
                return;
            }
            connection.on(eventName, handler);
        },
        off: (eventName, handler) => {
            if (!connection) return;
            connection.off(eventName, handler);
        },
        invoke: (methodName, ...args) => {
            if (!connection) {
                console.error("[chatHub] invoke() nhưng chưa có connection");
                return Promise.reject("No connection");
            }
            return connection.invoke(methodName, ...args);
        }
    };

    window[KEY] = api; // window.__chatHub = api;
})(window);
