# GitHub Repository Setup Guide

## Step 1: Configure Git Identity (if not already done)

Run these commands in your terminal to set your Git identity:

```bash
git config --global user.name "Your Name"
git config --global user.email "your.email@example.com"
```

Replace `Your Name` with your actual name and `your.email@example.com` with your GitHub email address.

## Step 2: Create Initial Commit

Your files are already staged. Once you've configured your Git identity, create the initial commit:

```bash
cd C:\Users\Anuj\Desktop\smallHR
git commit -m "Initial commit: SmallHR project"
```

## Step 3: Create a Private Repository on GitHub

### Option A: Using GitHub Web Interface (Recommended)

1. Go to [GitHub.com](https://github.com) and log in
2. Click the **"+" icon** in the top-right corner
3. Select **"New repository"**
4. Fill in the details:
   - **Repository name**: `smallHR` (or any name you prefer)
   - **Description**: (Optional) "SmallHR - HR Management System"
   - **Visibility**: Select **"Private"**
   - **DO NOT** initialize with README, .gitignore, or license (since you already have files)
5. Click **"Create repository"**

### Option B: Using GitHub CLI (if installed)

```bash
gh repo create smallHR --private --source=. --remote=origin --push
```

## Step 4: Add Remote and Push Code

After creating the repository on GitHub, you'll see instructions. Run these commands:

```bash
# Add the remote repository (replace YOUR_USERNAME with your GitHub username)
git remote add origin https://github.com/YOUR_USERNAME/smallHR.git

# Rename branch to 'main' (if needed)
git branch -M main

# Push your code to GitHub
git push -u origin main
```

**Note**: You may be prompted for your GitHub username and password/token:
- Username: Your GitHub username
- Password: Use a **Personal Access Token** (not your GitHub password)
  - Generate one at: https://github.com/settings/tokens
  - Select scope: `repo` (full control of private repositories)

## Alternative: Using SSH (if you have SSH keys set up)

If you prefer SSH authentication:

```bash
git remote add origin git@github.com:YOUR_USERNAME/smallHR.git
git push -u origin main
```

## Verification

After pushing, verify your code is on GitHub:

```bash
git remote -v
```

This should show your remote repository URL.

## Important Notes

- **Sensitive Data**: Make sure no sensitive information (passwords, API keys, connection strings with real credentials) is committed
- **.gitignore**: Already configured to exclude:
  - `node_modules/`
  - `bin/`, `obj/`
  - `.env` files
  - Build artifacts
  - User secrets

## Next Steps

Once your code is on GitHub, you can:
- Clone it on other machines: `git clone https://github.com/YOUR_USERNAME/smallHR.git`
- Work with branches: `git checkout -b feature-branch-name`
- Collaborate with others by adding them as collaborators in repository settings

