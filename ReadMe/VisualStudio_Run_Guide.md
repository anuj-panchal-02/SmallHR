# Running SmallHR Project in Visual Studio

This guide explains how to run both the backend API and frontend web application in Visual Studio.

## Prerequisites

1. **Visual Studio 2022** (or later) with the following workloads:
   - ASP.NET and web development
   - .NET desktop development (optional)

2. **Node.js** (v16 or later) - Download from [nodejs.org](https://nodejs.org/)

3. **.NET 8.0 SDK** - Should be included with Visual Studio 2022

## Running the Backend API (SmallHR.API)

### Method 1: Using Visual Studio (Recommended)

1. **Open the Solution**
   - Open Visual Studio 2022
   - Go to `File` → `Open` → `Project/Solution`
   - Navigate to `SmallHR.sln` and open it

2. **Set Startup Project**
   - In Solution Explorer, right-click on `SmallHR.API` project
   - Select `Set as Startup Project`

3. **Select Profile**
   - In the toolbar, you'll see a dropdown next to the run button
   - Select either:
     - **http** - Runs on `http://localhost:5192`
     - **https** - Runs on `https://localhost:7082` and `http://localhost:5192`

4. **Run the Project**
   - Press `F5` (Debug) or `Ctrl+F5` (Run without debugging)
   - The API will start and automatically open Swagger UI in your browser
   - API will be available at:
     - HTTP: `http://localhost:5192`
     - Swagger UI: `http://localhost:5192/swagger`

### Method 2: Using Visual Studio Developer Command Prompt

1. Open "Developer Command Prompt for VS 2022"
2. Navigate to the solution directory:
   ```cmd
   cd C:\Users\Anuj\Desktop\smallHR
   ```
3. Navigate to the API project:
   ```cmd
   cd SmallHR.API
   ```
4. Run the project:
   ```cmd
   dotnet run
   ```

## Running the Frontend (SmallHR.Web)

The frontend is a React/Vite application and needs to be run separately from Visual Studio.

### Option 1: Using Visual Studio's Integrated Terminal

1. In Visual Studio, go to `View` → `Terminal` (or press `Ctrl+``)
2. Navigate to the frontend directory:
   ```powershell
   cd SmallHR.Web
   ```
3. Install dependencies (first time only):
   ```powershell
   npm install
   ```
4. Start the development server:
   ```powershell
   npm run dev
   ```
5. The frontend will be available at: `http://localhost:5173/`

### Option 2: Using External Terminal/Command Prompt

1. Open PowerShell or Command Prompt
2. Navigate to the frontend directory:
   ```powershell
   cd C:\Users\Anuj\Desktop\smallHR\SmallHR.Web
   ```
3. Install dependencies (first time only):
   ```powershell
   npm install
   ```
4. Start the development server:
   ```powershell
   npm run dev
   ```

## Running Both Projects Together

### Recommended Approach: Two Windows

1. **Visual Studio Window**: Run the backend API (F5)
2. **Terminal Window**: Run the frontend (`npm run dev` in `SmallHR.Web`)

This allows you to:
- See backend logs in Visual Studio's Output/Debug window
- See frontend logs in the terminal
- Debug the backend in Visual Studio
- Use hot-reload for the frontend

### Alternative: Configure Multiple Startup Projects

You can configure Visual Studio to run both projects, but since the frontend is a Node.js project, you'll need to:

1. Right-click the solution in Solution Explorer
2. Select `Properties`
3. Under `Startup Project`, select `Multiple startup projects`
4. Set `SmallHR.API` to `Start`
5. For the frontend, you'll still need to run it via terminal since it's not a .NET project

## Project URLs

Once both projects are running:

- **Backend API**: `http://localhost:5192`
- **Swagger UI**: `http://localhost:5192/swagger`
- **Frontend**: `http://localhost:5173`

## Troubleshooting

### Backend Issues

1. **Port Already in Use**
   - If port 5192 is in use, change it in `launchSettings.json`
   - Or stop the process using that port

2. **Database Connection Issues**
   - Ensure your database connection string is correct in `appsettings.json`
   - Run migrations if needed:
     ```powershell
     cd SmallHR.API
     dotnet ef database update
     ```

3. **Build Errors**
   - Clean and rebuild the solution:
     - `Build` → `Clean Solution`
     - `Build` → `Rebuild Solution`

### Frontend Issues

1. **Node Modules Not Found**
   - Run `npm install` in the `SmallHR.Web` directory

2. **Port 5173 Already in Use**
   - Vite will automatically try the next available port
   - Or specify a different port:
     ```powershell
     npm run dev -- --port 3000
     ```

3. **API Connection Issues**
   - Ensure the backend API is running on `http://localhost:5192`
   - Check `src/services/api.ts` for the correct API base URL

## Debugging Tips

### Backend Debugging

- Set breakpoints in your C# code
- Press `F5` to start debugging
- Use Visual Studio's debugging tools (watch, locals, call stack)

### Frontend Debugging

- Use browser DevTools (F12)
- Set breakpoints in browser DevTools
- Or use VS Code with Chrome Debugger extension if preferred

## Environment Configuration

### Backend Environment Variables

- Development settings: `appsettings.Development.json`
- Production settings: `appsettings.json`
- User Secrets (for sensitive data): Right-click project → Manage User Secrets

### Frontend Environment Variables

- API base URL: `src/services/api.ts` (line 24)
- Currently set to: `http://localhost:5192/api`

## Next Steps

1. Ensure both projects are running
2. Open the frontend in your browser: `http://localhost:5173`
3. Try logging in (check `ReadMe/Frontend/LOGIN_CREDENTIALS.md` for test credentials)
4. Use Swagger UI to test API endpoints: `http://localhost:5192/swagger`

