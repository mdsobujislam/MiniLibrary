Setup & Run Instructions
This section guides you on how to get the project up and running on your local machine.
Prerequisites
•	.NET 8 SDK or later installed
•	SQL Server instance running (can be local or remote)
•	IDE like Visual Studio 2022 or VS Code
Steps to Setup
1.	Clone the repository:
2.	2.	git clone https://github.com/mdsobujislam/MiniLibrary.git
3.	cd mini-library
4.	Configure the database:
o	Open appsettings.json file.
o	Update the connection string under ConnectionStrings:DefaultConnection to point to your SQL Server instance. Example:
o	"ConnectionStrings": {
o	  "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=MiniLibraryDb;Trusted_Connection=True;"
o	}
5.	Create the database schema:
o	Run the SQL script located in Database/InitScript.sql on your SQL Server instance to create necessary tables and seed data.
6.	Restore dependencies:
7.	dotnet restore
8.	Run the project:
9.	dotnet run
The API will be available at https://localhost:5001 (or the port shown in your console).
10.	Open Swagger UI:
o	Navigate to https://localhost:5001/swagger in your browser to explore and test API endpoints.
________________________________________
Sample Login Credentials
To access the protected APIs, you must first authenticate and obtain a JWT token.
Credentials
Username	Password
admin	123456
How to login
•	API Endpoint: POST /api/Auth/login
•	Request Body Example:
•	{
•	  "username": "admin",
•	  "password": "123456"
•	}
•	Response:
o	On successful login, you will receive a JWT token in the response body:
o	{
o	  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
o	}
Use this token in the Authorization header (with the prefix Bearer ) for all other API requests.
________________________________________
API Workflow Documentation
This section explains the main API workflows and how the system operates.
1. Authentication
•	User sends login credentials to /api/Auth/login.
•	Server validates and returns a JWT token.
•	Token must be sent in the header of all subsequent requests:
•	Authorization: Bearer {token}
2. Book Management
•	Add Book:
POST /api/Books with book details JSON in the body.
Books have fields like Title, Author, ISBN, Category, CopiesAvailable, PublishedYear, Status.
•	Update Book:
PUT /api/Books/{id} to update existing book details.
•	Delete Book:
DELETE /api/Books/{id} for soft deletion (marks as deleted).
•	List Books:
GET /api/Books supports pagination and filtering by Title, Category, ISBN using query parameters.
•	Status logic:
When CopiesAvailable is zero, Status becomes Not Available automatically.
________________________________________
3. Member Management
•	Add Member:
POST /api/Members
•	Update Member:
PUT /api/Members/{id}
•	Delete Member:
DELETE /api/Members/{id} (soft delete)
•	List Members:
GET /api/Members
________________________________________
4. Borrowing Module
•	Borrow Books:
POST /api/Borrowings with MemberId and list of BookIds.
Reduces CopiesAvailable accordingly.
Validations: max 5 active borrowings per member, copies must be available.
•	Return Books:
POST /api/Borrowings/{borrowId}/return marks books as returned and updates CopiesAvailable.
•	Borrow Report:
GET /api/Borrowings/report?startDate=yyyy-MM-dd&endDate=yyyy-MM-dd returns stats:
o	Total Books Borrowed
o	Total Books Returned
o	Active Borrow Records
o	Most Borrowed Book
________________________________________
5. Error Handling
•	API returns appropriate HTTP status codes (400, 401, 404, 500) with error messages.
•	Authorization failures return 401 Unauthorized.

