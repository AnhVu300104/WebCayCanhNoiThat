$(document).ready(function () {
    let isChatbotOpen = false;
    let isFirstOpen = true;

    // Toggle chatbot visibility
    $('#chatbotai-button').click(function () {
        isChatbotOpen = !isChatbotOpen;

        if (isChatbotOpen) {
            $('#chatbotai-box').slideDown(300);
            $('#chatbotai-greeting').fadeOut(200);

            // Show welcome message on first open
            if (isFirstOpen) {
                isFirstOpen = false;
                setTimeout(function () {
                    addBotMessage("Xin chào! 🌱 Tôi là trợ lý tư vấn chăm sóc cây cảnh. Bạn có thể hỏi tôi về:\n\n• Cách chăm sóc cây\n• Tưới nước và bón phân\n• Xử lý sâu bệnh\n• Chọn cây phù hợp\n\nHãy đặt câu hỏi nhé! 😊");
                }, 500);
            }
        } else {
            $('#chatbotai-box').slideUp(300);
            setTimeout(function () {
                if (!isChatbotOpen) {
                    $('#chatbotai-greeting').fadeIn(200);
                }
            }, 300);
        }
    });

    // Auto-hide greeting after 8 seconds
    setTimeout(function () {
        $('#chatbotai-greeting').fadeOut(300);
    }, 8000);

    // Send message on button click
    $('#chatbotai-send').click(function () {
        sendMessage();
    });

    // Send message on Enter key
    $('#chatbotai-input').keypress(function (e) {
        if (e.which === 13) {
            e.preventDefault();
            sendMessage();
        }
    });

    function sendMessage() {
        const message = $('#chatbotai-input').val().trim();

        if (!message) {
            showNotification('Vui lòng nhập câu hỏi!', 'warning');
            return;
        }

        // Add user message to chat
        addUserMessage(message);
        $('#chatbotai-input').val('');

        // Show typing indicator
        showTypingIndicator();

        // Send to server
        $.ajax({
            url: '/ChatbotAI/SendMessage',
            type: 'POST',
            data: { message: message },
            success: function (response) {
                removeTypingIndicator();

                if (response.success) {
                    addBotMessage(response.message);
                } else {
                    addBotMessage('Xin lỗi, tôi gặp sự cố kỹ thuật. Vui lòng thử lại hoặc liên hệ hotline: 0964 155 923');
                }
            },
            error: function (xhr, status, error) {
                removeTypingIndicator();
                addBotMessage('⚠️ Không thể kết nối. Vui lòng kiểm tra kết nối mạng và thử lại!');
                console.error('Error:', error);
            }
        });
    }

    function addUserMessage(text) {
        const messageHtml = `
            <div class="chat-message user-message">
                <div class="message-bubble">${escapeHtml(text)}</div>
                <div class="message-avatar">👤</div>
            </div>
        `;
        $('#chatbotai-messages').append(messageHtml);
        scrollToBottom();
    }

    function addBotMessage(text) {
        // Convert line breaks to <br> for better display
        const formattedText = escapeHtml(text).replace(/\n/g, '<br>');

        const messageHtml = `
            <div class="chat-message bot-message">
                <div class="message-avatar">🤖</div>
                <div class="message-bubble">${formattedText}</div>
            </div>
        `;
        $('#chatbotai-messages').append(messageHtml);
        scrollToBottom();
    }

    function showTypingIndicator() {
        const typingHtml = `
            <div class="chat-message bot-message typing-message">
                <div class="message-avatar">🤖</div>
                <div class="typing-indicator">
                    <div class="typing-dot"></div>
                    <div class="typing-dot"></div>
                    <div class="typing-dot"></div>
                </div>
            </div>
        `;
        $('#chatbotai-messages').append(typingHtml);
        scrollToBottom();
    }

    function removeTypingIndicator() {
        $('.typing-message').remove();
    }

    function scrollToBottom() {
        const messagesContainer = $('#chatbotai-messages');
        messagesContainer.animate({
            scrollTop: messagesContainer[0].scrollHeight
        }, 300);
    }

    function escapeHtml(text) {
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return text.replace(/[&<>"']/g, function (m) { return map[m]; });
    }

    function showNotification(message, type) {
        // Simple notification - you can enhance this
        const notification = $('<div>')
            .css({
                position: 'fixed',
                top: '20px',
                right: '20px',
                padding: '12px 20px',
                background: type === 'warning' ? '#ff9800' : '#4caf50',
                color: 'white',
                borderRadius: '8px',
                zIndex: 10000,
                boxShadow: '0 4px 12px rgba(0,0,0,0.2)'
            })
            .text(message)
            .appendTo('body');

        setTimeout(function () {
            notification.fadeOut(300, function () {
                $(this).remove();
            });
        }, 3000);
    }
});