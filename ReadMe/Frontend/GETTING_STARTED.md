# Getting Started with SmallHR Frontend

## 🎉 Congratulations!

Your SmallHR frontend is now set up and ready to use! This guide will help you get started.

## ✅ What's Included

### **Pages**
- ✅ **Login Page** (`/login`) - Beautiful gradient login form
- ✅ **Dashboard** (`/dashboard`) - Employee overview with quick stats

### **Features Implemented**
- ✅ JWT Authentication with token storage
- ✅ Protected routes with authentication
- ✅ Responsive mobile-first design
- ✅ Type-safe API integration
- ✅ State management with Zustand
- ✅ Beautiful UI with Ant Design

## 🚀 Running the Application

### Start the Frontend
```bash
cd SmallHR.Web
npm run dev
```

The app will be available at: **http://localhost:5173**

### Start the Backend API
```bash
cd SmallHR.API
dotnet run
```

The API will be available at: **http://localhost:5192**

## 🔐 Login Credentials

Use the default admin account created by the seed data:

- **Email**: `admin@smallhr.com`
- **Password**: `Admin123!`

## 📖 User Guide

### 1. **Login**
1. Navigate to `http://localhost:5173`
2. You'll be redirected to the login page
3. Enter your credentials
4. Click "Log in"

### 2. **Dashboard**
After login, you'll see:
- **Quick Stats** - Total employees, pending leaves, attendance
- **Quick Actions** - Clock in/out buttons
- **Pending Leave Requests** - Table of requests awaiting approval

### 3. **Navigation**
- Dashboard is your home base
- More pages coming soon (Employees, Leave Requests, Attendance)

## 🎨 UI Features

### **Responsive Design**
- ✅ Mobile-friendly (works on phones & tablets)
- ✅ Touch-optimized buttons
- ✅ Adaptive layout

### **Modern UX**
- ✅ Smooth animations
- ✅ Hover effects
- ✅ Loading states
- ✅ Error messages
- ✅ Success notifications

### **Accessibility**
- ✅ Keyboard navigation
- ✅ Screen reader support
- ✅ High contrast
- ✅ ARIA labels

## 🛠️ Development

### **Adding a New Page**

1. Create the page component:
```typescript
// src/pages/Employees.tsx
export default function Employees() {
  return <div>Employees Page</div>;
}
```

2. Add the route in `App.tsx`:
```typescript
<Route
  path="/employees"
  element={
    <ProtectedRoute>
      <Employees />
    </ProtectedRoute>
  }
/>
```

### **Making API Calls**

```typescript
import { employeeAPI } from '../services/api';

// Get all employees
const response = await employeeAPI.getAll();
const employees = response.data;

// Create an employee
const newEmployee = await employeeAPI.create({
  employeeId: 'EMP001',
  firstName: 'John',
  lastName: 'Doe',
  // ... other fields
});
```

### **Using State Management**

```typescript
import { useAuthStore } from '../store/authStore';

function MyComponent() {
  const { user, isAuthenticated, logout } = useAuthStore();
  
  return (
    <div>
      {isAuthenticated && <p>Welcome, {user?.firstName}!</p>}
      <button onClick={logout}>Logout</button>
    </div>
  );
}
```

### **Form Handling**

```typescript
import { Form, Input, Button } from 'antd';

function MyForm() {
  const [form] = Form.useForm();
  
  const onFinish = (values) => {
    console.log('Form values:', values);
  };
  
  return (
    <Form form={form} onFinish={onFinish}>
      <Form.Item
        name="email"
        rules={[{ required: true, type: 'email' }]}
      >
        <Input placeholder="Email" />
      </Form.Item>
      <Button type="primary" htmlType="submit">
        Submit
      </Button>
    </Form>
  );
}
```

## 📱 Mobile Testing

### **Test on Your Phone**

1. Find your computer's IP address:
```bash
# Windows
ipconfig

# Mac/Linux
ifconfig
```

2. Update Vite config to allow network access (already configured)

3. Access from phone: `http://YOUR_IP:5173`

### **Responsive Breakpoints**
- **Mobile**: < 576px
- **Tablet**: 576px - 768px
- **Desktop**: 768px - 992px
- **Large**: > 992px

## 🎯 Next Steps

### **Pages to Build**
1. **Employees Page** - CRUD for employees
2. **Leave Requests Page** - Manage leave requests
3. **Attendance Page** - View and manage attendance
4. **Reports Page** - Analytics and reports
5. **Profile Page** - User profile management
6. **Settings Page** - App configuration

### **Features to Add**
1. **Navigation Menu** - Sidebar or top nav
2. **Notifications** - Real-time notifications
3. **Dark Mode** - Theme switching
4. **Search** - Global search functionality
5. **Filters** - Advanced filtering
6. **Export** - PDF/Excel exports
7. **Charts** - Data visualization

## 🐛 Troubleshooting

### **API Connection Issues**
```typescript
// Update API_BASE_URL in src/services/api.ts
const API_BASE_URL = 'http://localhost:5192/api';
```

### **CORS Errors**
The backend already has CORS configured. If you still see errors:
1. Check the API is running
2. Verify the URL matches
3. Check browser console for details

### **Build Errors**
```bash
# Clear cache and rebuild
rm -rf node_modules
npm install
npm run build
```

### **TypeScript Errors**
```bash
# Check types
npm run type-check
```

## 📚 Resources

### **Documentation**
- [React Docs](https://react.dev)
- [TypeScript Docs](https://www.typescriptlang.org/docs/)
- [Ant Design Docs](https://ant.design/components/overview/)
- [Vite Docs](https://vitejs.dev)
- [Zustand Docs](https://docs.pmnd.rs/zustand)

### **Component Examples**
- [Ant Design Components](https://ant.design/components/overview/)
- [Ant Design Pro](https://pro.ant.design/)

### **Code Examples**
Check out the existing pages:
- `src/pages/Login.tsx` - Form handling, auth
- `src/pages/Dashboard.tsx` - Data fetching, stats
- `src/services/api.ts` - API integration
- `src/store/authStore.ts` - State management

## 💡 Tips

1. **Use TypeScript** - It catches errors before runtime
2. **Component Size** - Keep components small and focused
3. **Reusability** - Create reusable components
4. **Loading States** - Always show loading indicators
5. **Error Handling** - Always handle errors gracefully
6. **Responsive First** - Test on mobile early
7. **Accessibility** - Use semantic HTML and ARIA

## 🤝 Need Help?

- Check the `README.md` for technical details
- Review existing code for examples
- Refer to Ant Design documentation
- Test changes incrementally

---

**Happy Coding! 🚀**

