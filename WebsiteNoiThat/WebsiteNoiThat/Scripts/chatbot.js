document.addEventListener("DOMContentLoaded", () => {
	const button = document.getElementById("chatbot-button");
	const box = document.getElementById("chatbot-box");
	const input = document.getElementById("chatbot-input");
	const send = document.getElementById("chatbot-send");
	const messages = document.getElementById("chatbot-messages");
	const greeting = document.getElementById("chatbot-greeting");

	let historyInterval = null;
	let inFlight = false;
	let lastMessageCount = 0;

	// Mở/đóng chat
	button.addEventListener("click", () => {
		const isVisible = box.style.display === "flex";
		box.style.display = isVisible ? "none" : "flex";

		if (!isVisible) {
			loadHistory(); // load ngay khi mở
			if (historyInterval) clearInterval(historyInterval);
			historyInterval = setInterval(loadHistory, 2000);
		} else {
			if (historyInterval) {
				clearInterval(historyInterval);
				historyInterval = null;
			}
		}
	});

	// Load toàn bộ lịch sử và render lại (giải pháp đơn giản, tránh duplicate)
	function loadHistory() {
		fetch("/Chatbot/GetUserMessages")
			.then(res => res.json())
			.then(data => {
				if (!data || data.error === "not_logged_in") {
					// nếu chưa đăng nhập, show 1 thông báo
					if (messages.children.length === 0) {
						addMessage("bot", "⚠️ Vui lòng <a href='/RegisterAndLogin/Login' style='color:blue;'>đăng nhập</a> để xem lịch sử chat.");
					}
					return;
				}

				if (!Array.isArray(data)) return;

				// Xóa toàn bộ và render lại từ data (đơn giản, nhất quán)
				messages.innerHTML = "";
				data.forEach(msg => {
					addMessage(msg.IsFromAdmin ? "bot" : "user", msg.MessageText, msg.Time);
				});
				lastMessageCount = data.length;
			})
			.catch(() => {
				if (messages.children.length === 0) {
					addMessage("bot", "⚠️ Không thể tải lịch sử chat.");
				}
			});
	}

	// Thêm tin nhắn vào giao diện (render HTML của bot)
	function addMessage(sender, text, time = "") {
		const wrapper = document.createElement("div");
		wrapper.className = sender === "bot" ? "chatbot-bot" : "chatbot-user";

		const bubble = document.createElement("div");
		bubble.className = "chatbot-bubble";
		// CHÚ Ý: server trả HTML (Content(..., "text/html")), nên dùng innerHTML
		bubble.innerHTML = text;

		const timeTag = document.createElement("div");
		timeTag.className = "chatbot-time";
		timeTag.textContent = time || new Date().toLocaleTimeString("vi-VN", {
			hour: "2-digit",
			minute: "2-digit"
		});

		wrapper.append(bubble, timeTag);
		messages.appendChild(wrapper);
		messages.scrollTop = messages.scrollHeight;
	}

	// Gửi tin nhắn
	function sendMessage() {
		const text = input.value.trim();
		if (!text || inFlight) return;

		// Hiển thị tin user ngay lập tức (UX tốt hơn)
		addMessage("user", escapeHtml(text));
		inFlight = true;
		send.disabled = true;
		input.disabled = true;
		input.value = "";

		fetch("/Chatbot/Ask", {
			method: "POST",
			headers: { "Content-Type": "application/x-www-form-urlencoded" },
			body: "message=" + encodeURIComponent(text)
		})
			.then(res => res.text()) // server trả HTML hoặc chuỗi rỗng
			.then(responseText => {
				// Nếu server trả trực tiếp HTML (không rỗng), render ngay bot reply
				// (loadHistory sẽ tải lại và render toàn bộ sau đó, nên để tránh duplicate,
				//  ta tải lại lịch sử bằng cách render toàn bộ từ server)
				// Nhưng để phản hồi nhanh, nếu responseText có nội dung, ta có thể tạm show nó:
				// Tuy nhiên vì loadHistory sẽ render lại, ta chỉ cần gọi loadHistory để đồng bộ.
				loadHistory();
			})
			.catch(() => {
				addMessage("bot", "⚠️ Lỗi khi gửi tin nhắn.");
			})
			.finally(() => {
				inFlight = false;
				send.disabled = false;
				input.disabled = false;
			});
	}

	// Escape HTML cho tin user (tránh user inject HTML vào đoạn hiển thị user)
	function escapeHtml(unsafe) {
		return unsafe
			.replace(/&/g, "&amp;")
			.replace(/</g, "&lt;")
			.replace(/>/g, "&gt;")
			.replace(/"/g, "&quot;")
			.replace(/'/g, "&#039;");
	}

	send.addEventListener("click", sendMessage);

	input.addEventListener("keydown", e => {
		if (e.key === "Enter" && !e.shiftKey) {
			e.preventDefault();
			sendMessage();
		}
	});

	// Hiển thị lời chào
	setTimeout(() => {
		if (greeting) {
			greeting.style.display = "block";
			setTimeout(() => greeting.style.display = "none", 5000);
		}
	}, 2000);
});
