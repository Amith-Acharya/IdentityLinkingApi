# Identity Linking API

This is a .NET 9 Web API built to handle the "BiteSpeed Identity Reconciliation" task. It merges multiple customer identities that share the same email or phone number.

## Problem Solved

Customers often use different email addresses or phone numbers for their orders. The goal of this API is to:
1. Detect if incoming records belong to the same person.
2. Link them together by establishing a `primary` contact and chaining others as `secondary`.
3. Return a unified, merged customer identity.

## Tech Stack
* **Framework:** .NET 9 ASP.NET Core Web API
* **Database O/RM:** Entity Framework Core
* **Database Provider:** SQLite (for simple local running). It can easily be swapped to PostgreSQL for production/Render deployment.

## Running Locally

### Prerequisites
* [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Steps
1. Clone or download this repository.
2. Navigate to the project directory:
   ```bash
   cd IdentityLinkingApi
   ```
3. Run the application:
   ```bash
   dotnet run
   ```
   *Note: This will automatically create an `identity.db` SQLite database file in the project folder and migrate it since `db.Database.EnsureCreated()` is called in `Program.cs`.*

4. Use the Swagger UI or Postman to test. The API runs on `http://localhost:5000` or `https://localhost:5001`.
   ENDPOINT: `POST /Identify`

## JSON Payload Example

**Request:**
```json
{
  "email": "mcfly@hillvalley.edu",
  "phoneNumber": "123456"
}
```

**Response:**
```json
{
  "contact": {
    "primaryContactId": 1,
    "emails": [
      "mcfly@hillvalley.edu"
    ],
    "phoneNumbers": [
      "123456"
    ],
    "secondaryContactIds": []
  }
}
```

## How the Logic Works (The Core Algorithm)

1. **Exact Match Search:** The code scans the `Contacts` table for any row that shares the provided `Email` OR `PhoneNumber`.
2. **Case 1 (No Match):** If nothing matches, a new `primary` contact is created and returned.
3. **Case 2 & 3 (Matches Found):**
   * The system extracts the IDs of the root `primary` contacts for all intersecting records.
   * It loads **all** contacts (primary & secondary) that belong to these groups.
   * It sorts them by `CreatedAt` to find the absolute **oldest** contact. This oldest contact is crowned the one true `primary`.
   * For any other `primary` contacts that were caught in the crossfire (e.g., someone connecting an old email with an old phone number for the first time), they are downgraded to `secondary` and their `LinkedId` is updated to point to the oldest contact.
   * If the payload introduces a completely **new** email or phone number that isn't already inside the connected cluster, a new `secondary` contact is appended to the cluster.
   * The response consolidates the emails and phone numbers, ensuring the oldest primary's info always appears first.

## Deploying to Render with PostgreSQL

To meet the requirement of deploying this to Render with a PostgreSQL database, follow these steps:

1. **Switch EF Core Provider:**
   Replace the SQLite package with the Postgres provider:
   ```bash
   dotnet remove package Microsoft.EntityFrameworkCore.Sqlite
   dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
   ```

2. **Update `Program.cs`:**
   Change the `DbContext` configuration to use Postgres:
   ```csharp
   // Replace this:
   // builder.Services.AddDbContext<ApplicationDbContext>(options =>
   //     options.UseSqlite("Data Source=identity.db"));

   // With this:
   builder.Services.AddDbContext<ApplicationDbContext>(options =>
       options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
   ```

3. **Configure Connection String:**
   Add your Render PostgreSQL connection string to `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=your-render-db-host;Database=yourdb;Username=youruser;Password=yourpass"
   }
   ```

4. **Deploy:**
   * Push your code to GitHub.
   * Go to [Render](https://render.com/) > New Web Service > Select your repository.
   * Set the Environment to `Docker` or `ASP.NET Core` (You can easily add a standard `Dockerfile` if needed).
   * Ensure your Database is deployed in the same Render region for faster queries.
