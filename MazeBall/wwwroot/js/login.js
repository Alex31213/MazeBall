const baseUrl = window.location.protocol + "//" + window.location.host;

let loginButton = document.getElementById("loginButton");
let registerButton = document.getElementById("registerButton");
let usernameField = document.getElementById("usernameField");
let emailField = document.getElementById("emailField");
let passwordField = document.getElementById("passwordField");
let confirmPasswordField = document.getElementById("confirmPasswordField");
let datepickerField = document.getElementById("datepickerField");
let title = document.getElementById("title");
let errorText = document.getElementById("errorText");
let birthDateText = document.getElementById("birthDateText");

emailField.style.maxHeight = "0";
confirmPasswordField.style.maxHeight = "0";
datepickerField.style.maxHeight = "0";
birthDateText.style.maxHeight = "0";

loginButton.onclick = function () {
    if (loginButton.classList.contains('disabled')) {
        emailField.style.maxHeight = "0";
        confirmPasswordField.style.maxHeight = "0";
        datepickerField.style.maxHeight = "0";
        birthDateText.style.maxHeight = "0";
        title.innerHTML = "Log In";
        registerButton.classList.add("disabled");
        loginButton.classList.remove("disabled");
        errorText.innerHTML = " ";
    }
    else if (!loginButton.classList.contains('disabled')) {
        if (usernameField.querySelector('input').value == "") {
            errorText.innerHTML = "Username field is empty.";
        }
        else if (passwordField.querySelector('input').value == "") {
            errorText.innerHTML = "Password field is empty.";
        }
        else {
            fetch(baseUrl + '/login', {
                method: "POST",
                headers: {
                    'Content-Type': 'application/json;charset=UTF-8',
                },
                body: JSON.stringify({
                    username: usernameField.querySelector('input').value,
                    password: passwordField.querySelector('input').value
                })
            })
                .then(response => {
                    if (response.ok) {
                        return response.json();
                    } else {
                        return response.json().then(error => { throw error; });
                    }
                })
                .then(data => {
                    console.log(data);
                    sessionStorage.setItem('token', data.token);
                    window.location.replace(baseUrl + "/lobby");
                })
                .catch(error => {
                    console.error(error);
                    errorText.innerHTML = error;
                });
        }
    }
}

registerButton.onclick = function () {
    if (registerButton.classList.contains('disabled')) {
        emailField.style.maxHeight = "65";
        confirmPasswordField.style.maxHeight = "65";
        datepickerField.style.maxHeight = "65";
        birthDateText.style.maxHeight = "65";
        title.innerHTML = "Register";
        registerButton.classList.remove("disabled");
        loginButton.classList.add("disabled");
        errorText.innerHTML = " ";
    }
    else if (!registerButton.classList.contains('disabled')) {
        if (passwordField.querySelector('input').value == confirmPasswordField.querySelector('input').value &&
            passwordField.querySelector('input').value != "") {
            fetch(baseUrl + '/register', {
                method: "POST",
                headers: {
                    'Content-Type': 'application/json;charset=UTF-8'
                },
                body: JSON.stringify({
                    username: usernameField.querySelector('input').value,
                    email: emailField.querySelector('input').value,
                    password: passwordField.querySelector('input').value,
                    birthdate: datepickerField.querySelector('input').value
                })
            })
                .then(response => {
                    if (response.ok) {
                        emailField.style.maxHeight = "0";
                        confirmPasswordField.style.maxHeight = "0";
                        datepickerField.style.maxHeight = "0";
                        birthDateText.style.maxHeight = "0";
                        title.innerHTML = "Log In";
                        registerButton.classList.add("disabled");
                        loginButton.classList.remove("disabled");
                        errorText.innerHTML = " ";
                        return response.json();
                    } else {
                        return response.json().then(error => { throw error; });
                    }
                })
                .then(data => {
                    console.log(data);
                })
                .catch(error => {
                    console.error(error);
                    errorText.innerHTML = error;
                });
        }
        else if (usernameField.querySelector('input').value == "") {
            errorText.innerHTML = "Username field is empty.";
        }
        else if (emailField.querySelector('input').value == "") {
            errorText.innerHTML = "Email field is empty.";
        }
        else if (passwordField.querySelector('input').value == "") {
            errorText.innerHTML = "Password field is empty.";
        }
        else if (confirmPasswordField.querySelector('input').value == "") {
            errorText.innerHTML = "Confirm Password field is empty.";
        }
        else if (passwordField.querySelector('input').value != confirmPasswordField.querySelector('input').value) {
            errorText.innerHTML = "Passwords don't match. Try again.";
        }
    }
}

$(function () {
    $("#datepickerField").datepicker({
        autoclose: true,
        todayHighlight: true,
    }).datepicker('update', new Date());
});