const baseUrl = window.location.protocol + "//" + window.location.host;
const lobbyUrl = baseUrl + "/lobby";
var finishedGame = false;
var isRedirecting = false;

var chatButtonTypes = [
    "enabled",
    "disabled"
]

function checkToken() {
    fetch(baseUrl + '/checkToken', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            token: sessionStorage.getItem('token')
        })
    })
        .then(response => {
            if (!response.ok) {
                throw new Error("Token invalid!");
            }

        })
        .catch(error => {
            console.error(error);
            window.location.replace(baseUrl);
        });
}

window.onload = checkToken();

setInterval(checkToken, 10000);

function getClaimsFromToken(token) {
    const tokenParts = token.split('.');
    if (tokenParts.length !== 3) {
        console.error('Invalid token.');
        return null;
    }
    const payload = tokenParts[1];
    try {
        const decodedPayload = atob(payload);
        const claims = JSON.parse(decodedPayload);
        return claims;
    } catch (err) {
        console.error('Error decoding token:', err);
        return null;
    }
}


function getUserProfileImage(username) {
    return fetch(baseUrl + '/getUserProfileImage', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': 'Bearer ' + sessionStorage.getItem('token')
        },
        body: JSON.stringify({
            username: username
        })
    })
        .then(response => {
            if (!response.ok) {
                throw new Error('Failed to fetch profile image.');
            }
            return response.blob();
        })
        .then(imageBlob => {
            return URL.createObjectURL(imageBlob);
        });
}

var connection = new signalR.HubConnectionBuilder().withUrl(window.location.href + "/gameHub", {
    accessTokenFactory: () => sessionStorage.getItem('token')
}).build();

function closeChatContainer() {
    const chatContainer = document.getElementById("chat-container");
    chatContainer.style.display = "none";
    const chatButton = document.getElementById('chat-button');
    chatButton.textContent = "Open Chat";
}

function openChatContainer() {
    const chatContainer = document.getElementById("chat-container");
    chatContainer.style.display = "block";
    const chatButton = document.getElementById('chat-button');
    chatButton.textContent = "Close Chat";
}

$(function () {
    connection.start().then(function () {
        console.log("SignalR connection established.");
        updateRooms();
    }).catch(function (err) {
        return console.error(err);
    });

    const chatButton = document.getElementById('chat-button');
    for (var j = 0; j < chatButtonTypes.length; j++) {
        chatButton.classList.remove(chatButtonTypes[j]);
    }
    chatButton.classList.add(chatButtonTypes[0]);
    chatButton.onclick = function () {
        const chatContainer = document.getElementById('chat-container');
        const computedStyle = getComputedStyle(chatContainer);
        const displayValue = computedStyle.getPropertyValue('display');
        if (displayValue == 'block') {
            closeChatContainer();
        }
        else if (displayValue == 'none') {
            openChatContainer();
        }
    };

    const sendChatTextButton = document.getElementById('sendChatText');
    sendChatTextButton.onclick = function () {
        const chatTextToSendField = document.getElementById('chatTextToSend');
        var userText = chatTextToSendField.value;
        if (userText.length > 0) {
            connection.invoke('SendChatMessage', sessionStorage.getItem('roomName'), userText);
            chatTextToSendField.value = '';
        }
    }
});

async function updatePlayersContainer(playersList) {
    const container = document.querySelector('.player-container');
    container.innerHTML = '';

    playersList.forEach((player) => {
        const playerName = player.username;
        const playerColor = player.color;
        const playerElement = document.createElement('div');
        playerElement.classList.add('player');

        const playerImg = document.createElement('img');
        playerImg.classList.add('player-img');
        getUserProfileImage(playerName).then(function (profileImage) {
            playerImg.src = profileImage;
            playerImg.alt = playerName;
        }).catch(function (error) {
            console.error('Failed to fetch profile image:', error);
        });
        playerElement.appendChild(playerImg);

        const playerNameElement = document.createElement('span');
        playerNameElement.classList.add('player-name');
        playerNameElement.textContent = playerName;
        playerElement.appendChild(playerNameElement);

        const colorSquare = document.createElement('div');
        colorSquare.classList.add('color-square');
        colorSquare.classList.add(playerColor);
        playerElement.appendChild(colorSquare);

        container.appendChild(playerElement);
    });
}

// Circle Angle

const circle = document.getElementById('circle');
const arrow = document.getElementById('arrow');
const angleDisplay = document.getElementById('angleText');

circle.addEventListener('mousemove', (e) => {
    if (e.buttons !== 1) return;

    const rect = circle.getBoundingClientRect();
    const centerX = rect.left + rect.width / 2;
    const centerY = rect.top + rect.height / 2;

    const dx = e.clientX - centerX;
    const dy = e.clientY - centerY;

    const angle = Math.atan2(dy, dx) * (180 / Math.PI) + 90;
    const fixedAngle = ((angle % 360) + 360) % 360;
    const displayAngle = Math.round(fixedAngle) % 360;

    arrow.style.transform = `translate(-50%, -100%) rotate(${angle}deg)`;
    angleDisplay.textContent = `Angle: ${displayAngle}°`;
});

// Power Bar

const powerRange = document.getElementById('powerRange');
const powerValue = document.getElementById('powerValue');

function updatePowerValue() {
    powerValue.textContent = 'Power: ' + powerRange.value + '%';
}

powerRange.addEventListener('input', updatePowerValue);

connection.on('updatePlayersContainer', function (playersList) {
    updatePlayersContainer(playersList);
    console.log("PlayersContainer update received");
});

connection.on('setRoomName', function (roomName) {
    sessionStorage.setItem('roomName', roomName);
});

connection.on('eventTextUpdate', function (eventText) {
    var roadElement = document.getElementById('event-text');
    roadElement.innerText = eventText;
});

connection.on("updateChat", messages => {
    const messageChatContainer = document.getElementById('message-chatContainer');
    messageChatContainer.innerHTML = '';
    for (var messageKey in messages) {
        var spanElement = document.createElement('span');
        spanElement.className = 'messageChat';
        spanElement.textContent = messages[messageKey];
        messageChatContainer.appendChild(spanElement);
    }
})

connection.on('endGame', winnerText => {
    const gameEndBackgroundPopup = document.createElement('div');
    gameEndBackgroundPopup.classList.add('gameEndBackgroundPopup');

    const gameEndPopupContent = document.createElement('div');
    gameEndPopupContent.classList.add('gameEndPopupContent');
    gameEndPopupContent.innerHTML = winnerText;

    gameEndBackgroundPopup.appendChild(gameEndPopupContent);
    document.body.appendChild(gameEndBackgroundPopup);

    gameEndBackgroundPopup.style.visibility = 'visible';
    gameEndBackgroundPopup.style.opacity = '1';

    const chatButton = document.getElementById('chat-button');
    chatButton.classList.add(chatButtonTypes[1]);
    chatButton.onclick = null;
    closeChatContainer();

    finishedGame = true;
    isRedirecting = true;

    setTimeout(function () {
        window.location.replace(lobbyUrl);
        console.error("Didn't leave the room");
    }, 5000);
})

$(window).on('beforeunload', function () {
    connection.stop();
    fetch(baseUrl + '/logout', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': 'Bearer ' + sessionStorage.getItem('token')
        }
    }).then(() => {
        if (!finishedGame && !isRedirecting) {
            sessionStorage.removeItem('token');
        }
        sessionStorage.removeItem('roomName');
    }).catch(error => {
        console.error(error);
    });
});

connection.onclose(error => {
    console.error(error);
    if (!finishedGame && !isRedirecting) {
        sessionStorage.removeItem('token');
        window.location.replace(baseUrl);
    }
    sessionStorage.removeItem('roomName');
})
