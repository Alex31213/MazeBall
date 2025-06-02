const logoutBaseUrl = window.location.protocol + "//" + window.location.host;
const logoutButton = document.getElementById('logoutButton');

logoutButton.addEventListener('click', function () {
    fetch(logoutBaseUrl + '/logout', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': 'Bearer ' + sessionStorage.getItem('token')
        }
    }).then(() => {
        sessionStorage.removeItem('token');
        window.location.replace(logoutBaseUrl);
    }).catch(error => {
        console.error(error);
    });
})

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
    return fetch(logoutBaseUrl + '/getUserProfileImage', {
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

$(window).on('load', function () {
    const claims = getClaimsFromToken(sessionStorage.getItem('token'));
    if (claims) {
        $('#usernameNavBar').text(claims.username);
        getUserProfileImage(claims.username)
            .then(imageUrl => {
                $('#loggedUserProfileImage').attr('src', imageUrl);
            })
            .catch(error => {
                console.error(error);
            });
    }
});

const loggedUserProfileImage = document.getElementById('loggedUserProfileImage');
loggedUserProfileImage.addEventListener('click', function () {
    const input = document.createElement('input');
    input.type = 'file';
    input.style.display = 'none';
    input.addEventListener('change', function (event) {
        const file = event.target.files[0];
        const formData = new FormData();
        formData.append('profileImage', file);
        fetch(logoutBaseUrl + '/uploadProfileImage', {
            method: 'POST',
            headers: {
                'Authorization': 'Bearer ' + sessionStorage.getItem('token')
            },
            body: formData
        })
            .then(response => {
                if (response.ok) {
                    console.log('Image uploaded successfully!');
                    const claims = getClaimsFromToken(sessionStorage.getItem('token'));
                    if (claims) {
                        $('#usernameNavBar').text(claims.username);
                        getUserProfileImage(claims.username)
                            .then(imageUrl => {
                                $('#loggedUserProfileImage').attr('src', imageUrl);
                            })
                            .catch(error => {
                                console.error(error);
                            });
                    }
                } else {
                    console.error('Error uploading image.');
                }
            })
            .catch(error => {
                console.error('Error sending the request:', error);
            });
    });
    document.body.appendChild(input);
    input.click();
    document.body.removeChild(input);
});

