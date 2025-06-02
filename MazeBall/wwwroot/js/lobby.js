const baseUrl = window.location.protocol + "//" + window.location.host;
var isRedirecting = false;

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

var connection = new signalR.HubConnectionBuilder().withUrl(window.location.href + "/lobbyHub", {
    accessTokenFactory: () => sessionStorage.getItem('token')
}).build();

function updateRooms() {
    connection.invoke("UpdateRooms")
        .then(() => {
            console.log("Rooms update called");
        })
        .catch((error) => {
            console.error("Error invoking rooms update", error);
        });
}

$(function () {
    connection.start().then(function () {
        console.log("SignalR connection established.");
        updateRooms();
    }).catch(function (err) {
        return console.error(err);
    });
});

$(window).on('beforeunload', function () {
    connection.stop();
    fetch(baseUrl + '/logout', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': 'Bearer ' + sessionStorage.getItem('token')
        }
    }).then(() => {
        if (!isRedirecting) {
            sessionStorage.removeItem('token');
        }
    }).catch(error => {
        console.error(error);
    });
});

connection.onclose(error => {
    console.error(error);
    if (!isRedirecting) {
        sessionStorage.removeItem('token');
        window.location.replace(baseUrl);
    }
})

function openCreateRoomPopup() {
    document.getElementById('createRoomPopup').classList.add('visible');
}

function closeCreateRoomPopup() {
    document.getElementById('createRoomPopup').classList.remove('visible');
}

function createExistsRoomPopup(existsRoomText) {
    const popupElement = document.createElement('div');
    popupElement.classList.add('popup', 'visible');

    const popupContentElement = document.createElement('div');
    popupContentElement.classList.add('popup-content');

    const closeButtonElement = document.createElement('button');
    closeButtonElement.classList.add('close-button');
    closeButtonElement.innerHTML = '&times;';

    const popupTextElement = document.createElement('p');
    popupTextElement.textContent = existsRoomText;

    closeButtonElement.addEventListener('click', function () {
        popupElement.classList.remove('visible');
        // Remove the pop-up element from the DOM after it is closed
        popupElement.remove();
    });

    popupContentElement.appendChild(closeButtonElement);
    popupContentElement.appendChild(popupTextElement);
    popupElement.appendChild(popupContentElement);

    document.body.appendChild(popupElement);
}

async function createRoom() {
    const roomName = document.getElementById('roomName').value;

    const response = await connection.invoke('CreateRoom', roomName, 2);

    if (response === 'RoomCreated') {
        console.log('Room created successfully.');
        closeCreateRoomPopup();
    } else if (response === 'RoomExists') {
        console.log('Room already exists.');
        closeCreateRoomPopup();
        createExistsRoomPopup('Room already exists.');
    } else if (response === 'RoomInGame') {
        console.log('A room with the same name is already in-game.');
        closeCreateRoomPopup();
        createExistsRoomPopup('A room with the same name is already in-game.');
    } else if (response === 'UserInAnotherRoom') {
        console.log('User is already in another room.');
        closeCreateRoomPopup();
        createExistsRoomPopup('You are already in another room.');
    } else {
        console.error('Unknown response:', response);
        closeCreateRoomPopup();
        createExistsRoomPopup('Unknown response:', response);
    }
}

async function joinRoom(roomName) {
    try {
        const response = await connection.invoke('JoinRoom', roomName);

        if (response === 'RoomJoined') {
            console.log(`Joined room '${roomName}' successfully.`);
        } else if (response === 'RoomNotFound') {
            console.log(`Room '${roomName}' does not exist.`);
            createExistsRoomPopup(`Room '${roomName}' does not exist.`);
        } else if (response === 'UserInAnotherRoom') {
            console.log(`User is already in another room.`);
            createExistsRoomPopup('You are already in another room.');
        } else if (response === 'UserAlreadyInRoom') {
            console.log(`User is already in room '${roomName}'.`);
            createExistsRoomPopup('You are already in that room.');
        } else if (response === 'RoomIsFull') {
            console.log(`Room '${roomName}' is already full. Cannot join.`);
            createExistsRoomPopup(`Room '${roomName}' is already full. Cannot join.`);
        } else {
            console.error('Unknown response:', response);
            createExistsRoomPopup('Unknown response:', response);
        }
    } catch (error) {
        console.error('Error joining room:', error);
    }
}


function leaveRoom() {
    connection.invoke('LeaveRoom')
        .catch(function (error) {
            console.error('Error leaving room:', error);
        });
}

function handleRoomsUpdate(roomsData, maxPlayers) {
    console.log("handleRoomUpdate");
    const containerElement = document.getElementById('container-lobby-rooms');
    containerElement.innerHTML = '';
    if (Object.keys(roomsData).length === 0) {
        const noRoomsTextElement = document.createElement('div');
        noRoomsTextElement.classList.add('no-rooms-text');
        noRoomsTextElement.textContent = 'No rooms available! Press "Create Room" to create a new room now!';
        containerElement.appendChild(noRoomsTextElement);
    } else {
        for (const roomName in roomsData) {
            const players = roomsData[roomName];

            const listItemElement = document.createElement('div');
            listItemElement.classList.add('list-item');

            const itemContentElement = document.createElement('div');
            itemContentElement.classList.add('item-content');

            const roomTitleElement = document.createElement('h3');
            const playersText = ` (${players.length}/${maxPlayers[roomName]})`;
            roomTitleElement.textContent = roomName + playersText;

            const inlineListElement = document.createElement('div');
            inlineListElement.classList.add('inline-list');

            players.forEach(function (player) {
                const imgContainer = document.createElement('div');
                const imgElement = document.createElement('img');
                const altElement = document.createElement('div');

                imgContainer.classList.add('profile-image-container');
                altElement.classList.add('alt-text');

                getUserProfileImage(player).then(function (profileImage) {
                    imgElement.src = profileImage;
                    imgElement.alt = player;
                    imgContainer.appendChild(imgElement);
                    imgContainer.appendChild(altElement);
                    inlineListElement.appendChild(imgContainer);
                }).catch(function (error) {
                    console.error('Failed to fetch profile image:', error);
                });

                imgContainer.addEventListener('mouseover', function () {
                    altElement.setAttribute('data-alt-text', player);
                    altElement.style.display = 'block';
                });

                imgContainer.addEventListener('mouseout', function () {
                    altElement.removeAttribute('data-alt-text');
                    altElement.style.display = 'none';
                });
            });


            const joinButtonElement = document.createElement('button');
            joinButtonElement.classList.add('button');
            joinButtonElement.textContent = 'Join Room';

            joinButtonElement.addEventListener('click', function () {
                joinRoom(roomName);
            });

            itemContentElement.appendChild(roomTitleElement);
            itemContentElement.appendChild(inlineListElement);
            itemContentElement.appendChild(joinButtonElement);
            listItemElement.appendChild(itemContentElement);
            containerElement.appendChild(listItemElement);
        }
    }
}

connection.on('updateRooms', function (roomsData, maxPlayers) {
    handleRoomsUpdate(roomsData, maxPlayers);
    console.log("Rooms update received");
});

connection.on('startGame', function () {
    console.log("Started game");
    isRedirecting = true;
    window.location.replace(baseUrl + "/game");
    console.error("Didn't enter the game");
});
