# 🎮 Interactive Web-Based Ball Maze Race  

An interactive **2D multiplayer web game**, where 2 players compete in a randomly generated maze.  
Each player controls a ball and, in alternating turns, plans their moves to be the first to reach the finish line.  
The game combines elements of **strategy, dynamics, and unpredictability** through collisions with walls or the opponent’s ball.  

---

## 🚀 Features  
- ✅ Account creation & authentication (username/password or Google account)  
- ✅ Profile management (profile picture, active/inactive status)  
- ✅ Lobby system: create, join, or leave a room  
- ✅ Procedural maze generation  
- ✅ **Turn-based mechanics** (adjustable shot angle and power)  
- ✅ **Manually implemented physics & collisions**, without external libraries  
- ✅ Integrated in-room chat  
- ✅ Match results saved in the database  

---

## 🛠️ Technologies  
- **Back-end**: C#, ASP.NET (.NET 6), SignalR (real-time communication), Entity Framework Core  
- **Front-end**: HTML5, CSS3, JavaScript  
- **Database**: Microsoft SQL Server  
- **Other tools**: JWT for authentication, LINQ, NuGet, Postman for API testing  

---

## 🏗️ Architecture  
- **Client-Server** with real-time communication via SignalR  
- **MVC (Model-View-Controller)** for separating data, application logic, and presentation  
- **Database First** approach for generating classes from SQL tables  

---

## 📖 How to Play  
1. **Create an account** → Register with username, email, and password  
2. **Log in** → Sign in with your account or Google  
3. **Enter a room** → Create a new room or join an existing one  
4. **Play** → Choose the angle and power of the ball’s shot to advance through the maze  
5. **Chat & compete** → Communicate with your opponent and race to the finish line  

---

## 📂 Project Structure  
- /ClientApp → web interface (HTML, CSS, JS)  
- /ServerApp → back-end logic (ASP.NET, SignalR, authentication)  
- /Database → SQL scripts and generated models  

---

## 📸 Screenshots  

**Log In:**  
<img width="626" height="346" alt="image" src="https://github.com/user-attachments/assets/e7b9b96b-e280-4758-ba42-12a7aac6b759" />

**Register:**  
<img width="626" height="346" alt="image" src="https://github.com/user-attachments/assets/94b57e16-5026-4add-b4c2-8ce902e02ce5" />

**Gameplay:**  
<img width="626" height="346" alt="image" src="https://github.com/user-attachments/assets/39843b1e-c2d0-47f1-83ea-67f5f36f7430" />

**Winner:**  
<img width="626" height="346" alt="image" src="https://github.com/user-attachments/assets/5a7b99c8-a0cb-4d72-bf4f-676ab5c4b6e9" />

---
