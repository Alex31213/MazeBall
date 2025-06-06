const baseUrl = window.location.protocol + "//" + window.location.host;
const lobbyUrl = baseUrl + "/lobby";
var finishedGame = false;
var isRedirecting = false;

const chatButtonTypes = [
    "enabled",
    "disabled"
]

const ballColorTypes = [
    "red",
    "purple"
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
    }).catch(function (err) {
        return console.error(err);
    });

    const turnButton = document.getElementById('turn-button');
    turnButton.disabled = true;

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

// Maze Generation

var mazeMatrix;
const cellSize = 12;
let chosenAngle = -Math.PI / 2;
let chosenPower = 0;
const maxPower = 10;
const wallVelocity = 0.9;
const normalVelocity = 0.97;
const ballBounceFactor = 0.9;

const maze = document.getElementById("maze");

const ballsCount = 2;
var balls;
var activeBall;

let animationRunning = true;
let animationId;

function convertToMatrix(generatedMaze) {
    const rowKeys = Object.keys(generatedMaze).sort((a, b) => Number(a) - Number(b));

    const mazeMatrix = rowKeys.map(rowKey => {
        const rowObj = generatedMaze[rowKey];
        const colKeys = Object.keys(rowObj).sort((a, b) => Number(a) - Number(b));

        return colKeys.map(colKey => rowObj[colKey]);
    });

    return mazeMatrix;
}

function gridToPixels(row, col) {
    return { x: col * cellSize, y: row * cellSize };
}

function createBallElement(id) {
    const ball = document.createElement("div");
    ball.classList.add("ball");
    ball.id = id;
    ball.style.position = "absolute";
    ball.style.width = `${cellSize}px`;
    ball.style.height = `${cellSize}px`;
    ball.style.borderRadius = "50%";
    ball.style.backgroundColor = ballColorTypes[id];
    return ball;
}

function createMaze() {
    maze.innerHTML = "";
    maze.style.width = `${mazeMatrix[0].length * cellSize}px`;
    maze.style.height = `${mazeMatrix.length * cellSize}px`;

    for (let row = 0; row < mazeMatrix.length; row++) {
        for (let col = 0; col < mazeMatrix[0].length; col++) {
            const cell = document.createElement("div");
            cell.classList.add("cell");
            cell.style.top = `${row * cellSize}px`;
            cell.style.left = `${col * cellSize}px`;
            cell.style.width = `${cellSize}px`;
            cell.style.height = `${cellSize}px`;

            const value = mazeMatrix[row][col];
            if (value === 1) cell.classList.add("wall");
            if (value === 2) cell.classList.add("wallFinish");

            maze.appendChild(cell);

            if (value === 3 || value == 4) {
                const { x, y } = gridToPixels(row, col);
                balls[value - 3].x = x;
                balls[value - 3].y = y;
            }
        }
    }

    // Ball Creation
    for (let i = 0; i < ballsCount; i++) {
        balls[i].el = createBallElement(i);
        maze.appendChild(balls[i].el);
        balls[i].el.style.transform = `translate(${balls[i].x}px, ${balls[i].y}px)`;
    }

}

function initBalls(numBalls) {
    const balls = [];

    for (let i = 0; i < numBalls; i++) {
        balls.push({
            id: i,
            x: 0,
            y: 0,
            vx: 0,
            vy: 0,
            el: null
        });
    }

    return balls;
}

function isWallAt(x, y) {
    const col = Math.floor(x / cellSize);
    const row = Math.floor(y / cellSize);
    return mazeMatrix[row]?.[col] === 1;
}

function isWallNearby(cx, cy, radius, numPoints = 12) {
    for (let i = 0; i < numPoints; i++) {
        const angle = (i / numPoints) * 2 * Math.PI;
        const checkX = cx + Math.cos(angle) * radius;
        const checkY = cy + Math.sin(angle) * radius;
        if (isWallAt(checkX, checkY)) return true;
    }
    return false;
}

function updateBall(ball) {
    if (Math.abs(ball.vx) < 0.01 && Math.abs(ball.vy) < 0.01) return;

    const radius = cellSize / 2;
    const cx = ball.x + radius;
    const cy = ball.y + radius;
    const precision = 12;

    let nextX = ball.x + ball.vx;
    if (isWallNearby(cx + ball.vx, cy, radius, precision)) {
        ball.vx = -ball.vx * wallVelocity;
        nextX = ball.x;
    }
    ball.x = nextX;

    let nextY = ball.y + ball.vy;
    if (isWallNearby(cx, cy + ball.vy, radius, precision)) {
        ball.vy = -ball.vy * wallVelocity;
        nextY = ball.y;
    }
    ball.y = nextY;

    ball.vx *= normalVelocity;
    ball.vy *= normalVelocity;

    ball.el.style.transform = `translate(${ball.x}px, ${ball.y}px)`;
}

function correctWallCollision(ball) {
    const radius = cellSize / 2;
    const cx = ball.x + radius;
    const cy = ball.y + radius;
    const precision = 12;

    if (isWallNearby(cx, cy, radius, precision)) {
        ball.x -= ball.vx;
        ball.y -= ball.vy;

        ball.vx = -ball.vx * wallVelocity;
        ball.vy = -ball.vy * wallVelocity;

        ball.el.style.transform = `translate(${ball.x}px, ${ball.y}px)`;
    }
}

function handleBallCollision(b1, b2) {
    const dx = b2.x - b1.x;
    const dy = b2.y - b1.y;
    const distance = Math.sqrt(dx * dx + dy * dy);
    const minDistance = cellSize;

    if (distance < minDistance) {
        const angle = Math.atan2(dy, dx);
        const overlap = minDistance - distance;
        const moveX = Math.cos(angle) * (overlap / 2);
        const moveY = Math.sin(angle) * (overlap / 2);
        const radius = cellSize / 2;

        const b1CanMove = !isWallNearby(b1.x - moveX + radius, b1.y - moveY + radius, radius);
        const b2CanMove = !isWallNearby(b2.x + moveX + radius, b2.y + moveY + radius, radius);

        if (b1CanMove && b2CanMove) {
            b1.x -= moveX;
            b1.y -= moveY;
            b2.x += moveX;
            b2.y += moveY;
        } else if (b1CanMove && !b2CanMove) {
            b1.x -= moveX * 2;
            b1.y -= moveY * 2;
        } else if (!b1CanMove && b2CanMove) {
            b2.x += moveX * 2;
            b2.y += moveY * 2;
        }

        // Relative speeds
        const vxTotal = b1.vx - b2.vx;
        const vyTotal = b1.vy - b2.vy;
        const impactSpeed = vxTotal * Math.cos(angle) + vyTotal * Math.sin(angle);

        if (impactSpeed > 0) {
            const impulse = impactSpeed * ballBounceFactor;

            if (!b1CanMove && b2CanMove) {
                // Only ball2 can move
                b2.vx += impulse * Math.cos(angle);
                b2.vy += impulse * Math.sin(angle);
            } else if (b1CanMove && !b2CanMove) {
                // Only ball1 can move
                const normalX = Math.cos(angle);
                const normalY = Math.sin(angle);
                const dot = b1.vx * normalX + b1.vy * normalY;

                b1.vx -= 2 * dot * normalX * ballBounceFactor;
                b1.vy -= 2 * dot * normalY * ballBounceFactor;
            } else {
                // Both balls can move
                b1.vx -= impulse * Math.cos(angle);
                b1.vy -= impulse * Math.sin(angle);
                b2.vx += impulse * Math.cos(angle);
                b2.vy += impulse * Math.sin(angle);
            }
        }
    }
}

function sendBallOnFinish(id) {
    const currentBallId = id + 1;
    connection.invoke('CheckVictory', sessionStorage.getItem('roomName'), currentBallId)
        .catch(error => {
            console.error(error);
        });
}

function isOnFinish() {
    const radius = cellSize / 2;

    for (let i = 0; i < ballsCount; i++) {
        const cx = balls[i].x + radius;
        const cy = balls[i].y + radius;
        const col = Math.floor(cx / cellSize);
        const row = Math.floor(cy / cellSize);
        if (mazeMatrix[row]?.[col] === 2) {
            sendBallOnFinish(i);
            return true;
        }
    }

    return false;
}

function isNoMovingBall() {
    for (let i = 0; i < ballsCount; i++) {
        if (balls[i].vx != 0 || balls[i].vy != 0) {
            return false;
        }
    }

    return true;
}

function sendBallsPositions() {
    const positions = balls.map(ball => [ball.x, ball.y]);
    console.log("Reached sendBallPositions");

    connection.invoke('CheckFinalPositions', sessionStorage.getItem('roomName'), positions)
        .catch(error => {
            console.error(error);
        });
}

const movingThreshold = 0.01;

function MakeBallsFullyStop() {
    for (let i = 0; i < ballsCount; i++) {
        if (Math.abs(balls[i].vx) < movingThreshold && Math.abs(balls[i].vy) < movingThreshold) {
            balls[i].vx = 0;
            balls[i].vy = 0;
        }
    }
}

function gameLoop() {
    if (!animationRunning) return;

    for (let i = 0; i < ballsCount; i++) {
        updateBall(balls[i]);
    }

    handleBallCollision(balls[0], balls[1]);

    for (let i = 0; i < ballsCount; i++) {
        updateBall(balls[i]);
    }

    if (isOnFinish()) {
        animationRunning = false;
        console.log("Finish reached! Stopping Animation.");
        return;
    }

    MakeBallsFullyStop();

    if (isNoMovingBall()) {
        animationRunning = false;
        sendBallsPositions();
    }

    animationId = requestAnimationFrame(gameLoop);
}

function MoveTurnBalls(activeBallId, vx, vy) {
    activeBall = balls[activeBallId];
    activeBall.vx = vx;
    activeBall.vy = vy;

    animationRunning = true;
    gameLoop();
}

document.addEventListener("keydown", e => {
    if (e.code === "Space") {
        const angle = chosenAngle;
        const power = maxPower * chosenPower;
        activeBall.vx = Math.cos(angle) * power;
        activeBall.vy = Math.sin(angle) * power;
    }
    if (e.code === "Tab") {
        e.preventDefault();
        activeBall = activeBall === balls[0] ? balls[1] : balls[0];
    }
});

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

    chosenAngle = Math.atan2(dy, dx);
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
    chosenPower = powerRange.value / 100;
}

powerRange.addEventListener('input', updatePowerValue);

// Event Text Update

function updateEventText(newString) {
    var roadElement = document.getElementById('event-text');
    roadElement.innerText = newString;
}

// Turn Button

function onClickTurnButton() {
    if (chosenPower == 0) {
        updateEventText("Select a power greater than 0% to shoot")
        return;
    }

    const angle = chosenAngle;
    const power = maxPower * chosenPower;
    activeBall.vx = Math.cos(angle) * power;
    activeBall.vy = Math.sin(angle) * power;

    const turnButton = document.getElementById('turn-button');
    turnButton.classList.remove('enabled');
    turnButton.classList.add('disabled');
    turnButton.disabled = true;
    turnButton.removeEventListener('click', onClickTurnButton);

    connection.invoke('SendTurnActiveBallPositions', sessionStorage.getItem('roomName'), activeBall.vx, activeBall.vy);
}

connection.on('startTurnButton', (currentPlayerBallId) => {
    const realBallId = currentPlayerBallId - 1;
    activeBall = balls[realBallId];

    const turnButton = document.getElementById('turn-button');
    turnButton.classList.remove('disabled');
    turnButton.classList.add('enabled');
    turnButton.onclick = null;
    turnButton.disabled = false;
    turnButton.addEventListener('click', onClickTurnButton);
});

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

connection.on('generateMaze', function (generatedMaze) {
    balls = initBalls(ballsCount);

    mazeMatrix = convertToMatrix(generatedMaze);
    createMaze();
    console.log("Generated Maze received");
});

connection.on('moveTurnBalls', function (activeBallId, vx, vy) {
    const realBallId = activeBallId - 1;

    MoveTurnBalls(realBallId, vx, vy);
    console.log("Move Balls request completed");
});



connection.on('endGame', winnerText => {
    animationRunning = false;

    const gameEndBackgroundPopup = document.createElement('div');
    gameEndBackgroundPopup.classList.add('gameEndBackgroundPopup');

    const gameEndPopupContent = document.createElement('div');
    gameEndPopupContent.classList.add('gameEndPopupContent');
    gameEndPopupContent.innerHTML = winnerText;

    gameEndBackgroundPopup.appendChild(gameEndPopupContent);
    document.body.appendChild(gameEndBackgroundPopup);

    gameEndBackgroundPopup.style.visibility = 'visible';
    gameEndBackgroundPopup.style.opacity = '1';

    const turnButton = document.getElementById('turn-button');
    turnButton.classList.remove('enabled');
    turnButton.classList.add('disabled');
    turnButton.disabled = true;
    turnButton.onclick = null;

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
