# CricZone Cricket Tournament System

A premium, real-time cricket tournament scoring and analytics platform built on ASP.NET Core and Microsoft SQL Server. CricZone provides a complete solution for cricket leagues to manage tournaments, register teams, input live ball-by-ball scoring and track leaderboards.

---

##  Key Features

### 1.  Live Active Match Ticker
* A modern, glowing marquee ticker banner positioned below the header.
* Broadcasts real-time score feeds of active matches utilizing **SignalR Web Sockets** so users see updates instantly without page refreshes.

### 2.  Dynamic Global Stats Dashboard
* Real-time metrics counters tracking **Teams Registered**, **Matches Played**, **Runs Scored**, and **Wickets Fallen** across all tournaments, dynamically calculated from your SQL Server database.

### 3.  Organizer Portal
* Sleek, glassmorphic auth panel supporting sliding **Sign In**, **Sign Up**, and **Forgot Password** recovery flows.
* Strict exactly **10-digit mobile number validations** with an expanded international country code selection dropdown.
* Strong security enforcing alphanumeric passwords (6-10 characters, letters & numbers, no special symbols) with salted SHA-256 password hashing.

### 4.  Tournament Leaderboards & Awards
* Auto-generated **Points Table** featuring standard **Net Run Rate (NRR)** calculations (correctly accounting for team and opponent bowled-out full allotted overs).
* Live award standings:
  *  **Orange Cap**: Top run scorers.
  *  **Purple Cap**: Top wicket takers.
  *  **Most Sixes**: Six hitters leaderboard.
  *  **Best Fielder**: Fielding points tracker (catches, run-outs, stumpings, direct hits).
  *  **Player of the Series**: Combined weighted score leaderboard.


##  Technology Stack

* **Backend Framework**: ASP.NET Core
* **Database & ORM**: Microsoft SQL Server & Entity Framework Core (EF Core)
* **Real-Time Communication**: ASP.NET Core SignalR (WebSockets)
* **Frontend**: HTML5 (Razor Views), CSS3 (Modern Glassmorphic UI), JavaScript

---

##  Getting Started

Setting up CricZone on your machine few minutes. Here's everything you need.

### Prerequisites
* **.NET 10.0 SDK**
* **Microsoft SQL Server**
* **SQL Server Management Studio (SSMS)**

### Setup Instructions

**Step 1: Get the Project on Your Machine**

 **Clone the Project**:
   Extract `CricZone_Tournament_Web.zip` to a folder wherever you'd like to keep it.

**Step 2: Point It to Your Database**
   Open `KplTournament.Web/appsettings.json` and update the connection string so it matches your SQL Server instance:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=CricZone;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Not sure what to put for `YOUR_SERVER_NAME`? It's usually something like:
- `Server=DESKTOP-FOK87FD\SQLEXPRESS`, or
- `Server=localhost\SQLEXPRESS`

You can find your exact server name at the top of Object Explorer in SSMS.

3. **Restore Packages & Build**:
   Open a terminal inside the project directory (`CricZone_Tournament_Web/KplTournament.Web`) and run:
   ```bash
   dotnet restore
   dotnet build
   ```
   This downloads everything the project needs and checks that it compiles cleanly.

4. **Run the Application**:
   Start the web server:
   ```bash
   dotnet run
   ```
   On startup, Entity Framework Core will automatically connect to SQL Server, create the **`CricZone`** database, generate all tables, and seed the default **`CricZone Tester Tournament`**!

5. **Launch the Portal**:
   Open your browser and navigate to:
   * **HTTPS**: `https://localhost:51605`
   * **HTTP**: `http://localhost:51606`

---

##  Project Architecture

* **`Controllers/`**: Contains endpoint handlers (`AdminController` for scoring/setup, `HomeController` for dashboards/leaderboards, `AccountController` for security).
* **`Models/`**: Core database schema entities containing relational mappings.
* **`Data/`**: AppDbContext configuration and `SeedData` initializer.
* **`Hubs/`**: SignalR hub for live scoring synchronization.
* **`Views/`**: UI views separated by feature folders.
