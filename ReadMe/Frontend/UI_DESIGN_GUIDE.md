# SmallHR UI/UX Design System

**Last Updated**: October 29, 2025  
**Status**: ✅ Production Ready

> **Note**: This is the single source of truth for all UI/UX standards. Always reference this file when implementing new features or making design changes.

---

## 📋 Table of Contents

1. [Design Philosophy](#design-philosophy)
2. [Color System](#color-system)
3. [Typography](#typography)
4. [Spacing & Layout](#spacing--layout)
5. [Border Radius](#border-radius)
6. [Components](#components)
7. [Animations](#animations)
8. [Responsive Design](#responsive-design)

---

## 🎨 Design Philosophy

### **Chrome-Inspired Modern Design**
- Clean, minimal interface with subtle curves
- Glass morphism effects with backdrop blur
- Smooth animations and micro-interactions
- Professional yet approachable aesthetic
- Vibrant gradients with purpose

### **Core Principles**
1. **Consistency**: Every element follows the same design language
2. **Clarity**: Clear visual hierarchy and readable typography
3. **Efficiency**: Quick access to important actions
4. **Modern**: Contemporary design trends without being trendy
5. **Accessible**: Readable colors and proper contrast ratios

---

## 🎨 Color System

### **Primary Colors**
```css
--color-primary: #4F46E5       /* Indigo - Main brand */
--color-primary-light: #818CF8 /* Light variant */
--color-primary-dark: #3730A3  /* Dark variant */
--color-accent: #06B6D4        /* Cyan - Accents */
--color-success: #10B981       /* Green - Success */
--color-warning: #F59E0B       /* Orange - Warning */
--color-error: #EF4444         /* Red - Error */
```

### **Gradients**
```css
--gradient-primary: linear-gradient(135deg, #667EEA 0%, #764BA2 100%)
--gradient-accent: linear-gradient(135deg, #06B6D4 0%, #3B82F6 100%)
--gradient-success: linear-gradient(135deg, #10B981 0%, #059669 100%)
--gradient-warning: linear-gradient(135deg, #F59E0B 0%, #D97706 100%)
--gradient-error: linear-gradient(135deg, #EF4444 0%, #DC2626 100%)
--gradient-info: linear-gradient(135deg, #3B82F6 0%, #2563EB 100%)
--gradient-surface: linear-gradient(180deg, #FFFFFF 0%, #F8FAFC 100%)
```

**Usage**: Use CSS variables for consistency:
- Primary actions: `background: 'var(--gradient-primary)'`
- Success actions: `background: 'var(--gradient-success)'`
- Warnings: `background: 'var(--gradient-warning)'`

### **Text Colors**
```css
--color-text-primary: #0F172A     /* Headings, important text */
--color-text-secondary: #64748B   /* Body text, labels */
--color-text-tertiary: #94A3B8    /* Muted text */
```

### **Icon Colors (Stats)**
- Teal: `#14B8A6` (Primary stats)
- Green: `#10B981` (Success/positive)
- Orange: `#FFA94D` (Warnings/attention)
- Cyan: `#06B6D4` (Info/secondary)
- Purple: `#8B5CF6` (Special/premium)

### **🌙 Dark Mode**

Dark mode is fully supported and can be toggled via the header button (bulb icon next to time).

**Dark Mode Colors**
```css
--color-base: #0F172A           /* Dark background */
--color-background: #0F172A     /* Main background */
--color-surface: #1E293B        /* Card/surface */
--color-text-primary: #F1F5F9   /* Light text */
--color-text-secondary: #CBD5E1 /* Secondary text */
--color-text-tertiary: #94A3B8  /* Tertiary text */

/* Adjusted brand colors for dark mode */
--color-primary: #818CF8        /* Lighter primary for contrast */
--color-accent: #22D3EE         /* Lighter accent */
--color-success: #34D399        /* Lighter success */
--color-warning: #FBBF24        /* Lighter warning */
--color-error: #F87171          /* Lighter error */

/* Dark mode gradients */
--gradient-primary: linear-gradient(135deg, #818CF8 0%, #A78BFA 100%)
--gradient-surface: linear-gradient(180deg, #1E293B 0%, #0F172A 100%)
```

**Implementation**
- Dark mode state is managed via `ThemeContext` (`src/contexts/ThemeContext.tsx`)
- Preference is persisted in `localStorage`
- CSS classes are automatically applied to `<html>` element
- All Ant Design components are styled for dark mode via `.dark-mode` CSS rules

**Toggle Button Location**
- Header: Right side, between time display and refresh button
- Icon: `BulbOutlined` (light mode) / `BulbFilled` (dark mode)
- Transition: Smooth 300ms cubic-bezier

---

## ✍️ Typography

### **Font Family**
```css
font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', sans-serif
```

### **Type Scale**

#### **Headings**
- **H1 (Page Titles)**: `40px / 700 / -0.02em` - Gradient text effect
- **H2 (Section Titles)**: `32px / 700 / -0.02em`
- **H3 (Card Titles)**: `18px / 600 / -0.01em` - Color: `#1E293B`

#### **Body Text**
- **Large**: `15px / 400 / normal`
- **Base**: `14px / 400 / normal`
- **Small**: `13px / 500 / normal` - Used for labels, stat titles
- **Tiny**: `12px / 500 / normal` - Used for meta info
- **Micro**: `11px / 600 / 0.5px` - Used for badges (uppercase)

#### **Stat Components**
- **Stat Title**: `13px / 500 / normal` - Color: `#64748B`
- **Stat Value**: `32px / 700 / -0.02em` - Color: `#1E293B`

---

## 📐 Spacing & Layout

### **Spacing System**
```css
--spacing-xs: 0.25rem   /* 4px */
--spacing-sm: 0.5rem    /* 8px */
--spacing-md: 1rem      /* 16px */
--spacing-lg: 1.5rem    /* 24px */
--spacing-xl: 2rem      /* 32px */
--spacing-2xl: 3rem     /* 48px */
--spacing-3xl: 4rem     /* 64px */
```

### **Layout Dimensions**

#### **Header & Sidebar**
- Header Height: `72px`
- Sidebar Width (Expanded): `280px`
- Sidebar Width (Collapsed): `80px`
- Toggle Button Position: `top: 16px`

#### **Padding Standards**
- **Main Container**: `32px 24px`
- **Header**: `0 24px`
- **Sidebar Logo**: `0 24px` (expanded)
- **Sidebar User Profile**: `20px 24px`
- **Sidebar Navigation**: `16px 12px`
- **Sidebar Bottom Actions**: `12px`
- **Stat Cards**: `28px` (all sides)
- **Table Cards**: `24px 32px 32px` (top, left/right, bottom)

#### **Grid System**
- **Row Gutters**: `[20, 20]` (horizontal, vertical)
- **Column Breakpoints**:
  - `xs: 24` (mobile - full width)
  - `sm: 12` (tablet - 2 columns)
  - `lg: 6` (desktop - 4 columns)

---

## 🔘 Border Radius

### **Modern Minimal Curves**
```css
--radius-sm: 0.375rem   /* 6px - Badges, small elements */
--radius-md: 0.5rem     /* 8px - Menu items, inputs */
--radius-lg: 0.625rem   /* 10px - Buttons, icons */
--radius-xl: 0.75rem    /* 12px - Cards */
--radius-2xl: 0.875rem  /* 14px - Large containers */
--radius-full: 9999px   /* Circular/pill */
```

### **Component Specific**
- **Cards (Stats & Tables)**: `12px`
- **Buttons (Action Buttons)**: `10px`
- **Menu Items**: `8px`
- **Inputs & Selects**: `8px`
- **Badges**: `10px`
- **Logo Icon**: `6px`

---

## 🧩 Components

### **Cards**

#### **Stats Cards**
```typescript
style={{
  borderRadius: 12,
  padding: 28,
  boxShadow: '0 2px 8px rgba(0, 0, 0, 0.06)',
  border: '1px solid rgba(148, 163, 184, 0.1)',
  background: 'var(--glass-background)',
}}
```

#### **Table Cards**
```typescript
style={{
  borderRadius: 12,
  boxShadow: '0 2px 8px rgba(0, 0, 0, 0.06)',
}}
bodyStyle={{ padding: '24px 32px 32px' }}
```

### **Buttons**

**⚠️ CRITICAL: Always use CSS variables for all buttons!**

#### **CSS Variables**
```css
/* All buttons MUST use these variables */
--button-height: 38px
--button-radius: 10px
--button-font-family: 'Inter', sans-serif
--button-font-weight: 500
--button-shadow-primary: 0 2px 6px rgba(79, 70, 229, 0.3)
--button-shadow-success: 0 2px 6px rgba(16, 185, 129, 0.2)
```

#### **Primary Action Button** ✅
```typescript
style={{
  borderRadius: 'var(--button-radius)',
  height: 'var(--button-height)',
  background: 'var(--gradient-primary)',
  fontFamily: 'var(--button-font-family)',
  fontWeight: 'var(--button-font-weight)',
  boxShadow: 'var(--button-shadow-primary)',
  border: 'none',
}}
```

#### **Success Button** ✅
```typescript
style={{
  borderRadius: 'var(--button-radius)',
  height: 'var(--button-height)',
  background: 'var(--gradient-success)',
  fontFamily: 'var(--button-font-family)',
  fontWeight: 'var(--button-font-weight)',
  boxShadow: 'var(--button-shadow-success)',
  border: 'none',
}}
```

#### **Secondary Button** ✅
```typescript
style={{
  borderRadius: 'var(--button-radius)',
  height: 'var(--button-height)',
  borderColor: 'var(--glass-border)',
  color: 'var(--color-text-secondary)',
  fontFamily: 'var(--button-font-family)',
  fontWeight: 'var(--button-font-weight)',
}}
```

**❌ NEVER hardcode these values - Always use CSS variables!**

### **Icons**
- **Stat Icons**: `fontSize: 20`
- **Sidebar Icons**: `fontSize: 17`
- **Header Icons**: `fontSize: 18`
- **Button Icons**: Match button font size

### **Sidebar Navigation**

#### **Menu Items**
```typescript
style={{
  padding: '11px 16px',  // expanded
  padding: '11px 8px',   // collapsed
  marginBottom: 4,
  borderRadius: 8,
  fontSize: 13,
  fontWeight: 600,  // active
  fontWeight: 500,  // inactive
}}

// Active State
background: 'var(--gradient-primary)'
color: '#FFFFFF'
boxShadow: '0 4px 12px rgba(79, 70, 229, 0.3)'

// Hover State
background: 'rgba(79, 70, 229, 0.06)'
color: '#4F46E5'
transform: 'translateX(2px)'
```

#### **Section Headers**
```typescript
style={{
  fontSize: 10,
  fontWeight: 700,
  color: '#94A3B8',
  textTransform: 'uppercase',
  letterSpacing: '0.8px',
  marginBottom: 8,
  paddingLeft: 16,
}}
```

### **Header**

#### **Simple & Clean**
- Right-aligned content
- Date & Time display with icon
- Refresh button with rotation on hover
- Fullscreen toggle
- User role badge with gradient

```typescript
// Header Container
style={{
  height: 72,
  padding: '0 24px',
  background: 'var(--glass-background)',
  backdropFilter: 'blur(12px)',
  borderBottom: '1px solid rgba(226, 232, 240, 0.8)',
}}
```

### **Tables**
- **Size**: `middle`
- **Page Size**: `10`
- **Border**: `none`
- **Margin Top**: `16px` (inside cards)
- **Pagination**: Bottom aligned, shows total count

---

## ✨ Animations

### **Transitions**
```css
/* Standard */
transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);

/* Layout Changes */
transition: width 250ms ease-in-out;
transition: margin-left 250ms ease-in-out;
```

### **Hover Effects**
- **Cards**: `translateY(-2px)` on hover
- **Buttons**: `translateY(-2px)` on hover
- **Menu Items**: `translateX(2px)` on hover
- **Toggle Button**: `scale(1.1)` on hover
- **Refresh Icon**: `rotate(180deg)` on hover

### **Micro-interactions**
- Smooth scale on hover for interactive elements
- Gentle shadow increase on elevation
- Smooth color transitions on state change

---

## 📱 Responsive Design

### **Breakpoints**
- **Mobile**: `< 768px`
- **Tablet**: `768px - 1200px`
- **Desktop**: `> 1200px`

### **Mobile Adjustments**
- All grids collapse to single column
- Container padding: `var(--spacing-xl) var(--spacing-md)`
- Title font size: `2rem`
- Stats center-aligned
- Sidebar becomes overlay

---

## 🎯 Critical Consistency Rules

### **Must Always Follow**
1. ✅ All stat cards: `borderRadius: 12`, `padding: 28`
2. ✅ All table cards: `borderRadius: 12`, `bodyStyle: '24px 32px 32px'`
3. ✅ All row gutters: `[20, 20]` with `marginBottom: 24`
4. ✅ All stat icons: `fontSize: 20`
5. ✅ All sidebar icons: `fontSize: 17`
6. ✅ All action buttons: `height: 38`, `borderRadius: 10`
7. ✅ All card titles: `fontSize: 18`, `fontWeight: 600`, `color: #1E293B`
8. ✅ All stat titles: `fontSize: 13`, `color: #64748B`
9. ✅ All stat values: `fontSize: 32`, `fontWeight: 700`
10. ✅ Header & Sidebar: Same height `72px`, same border color

---

## 🔍 Before Making Changes

### **Checklist**
- [ ] Does it follow the border radius system?
- [ ] Does it use the correct spacing scale?
- [ ] Does it use colors from the design system?
- [ ] Does it have proper hover states?
- [ ] Does it use the correct typography scale?
- [ ] Is it consistent with existing components?
- [ ] Does it work on mobile?
- [ ] Are animations smooth (250-300ms)?

### **When Adding New Components**
1. Check this guide first
2. Use CSS variables when available
3. Follow the spacing system
4. Apply consistent border radius
5. Add smooth transitions
6. Test hover states
7. Verify on mobile

---

## 📄 Page-Specific Guidelines

### **Role Permissions Page**
The Role Permissions page follows the same design system with some specific features:

**Layout:**
- Stats cards showing active permissions and unsaved changes
- Table with role-based access control matrix
- Toggle switches for each role-page combination
- Bulk save functionality with change tracking
- Help section with gradient background

**Color Coding:**
- SuperAdmin: Purple (`purple`)
- Admin: Red (`red`)
- HR: Orange (`orange`)
- Employee: Green (`green`)

**Features:**
- Real-time change tracking
- Bulk update to minimize API calls
- SuperAdmin permissions are always enabled (cannot be disabled)
- Initialize and Reset functions for first-time setup

---

## 📝 Quick Reference

### **Common Patterns**

#### **Stat Card**
```typescript
<Card
  bordered={false}
  style={{
    borderRadius: 12,
    boxShadow: '0 2px 8px rgba(0, 0, 0, 0.06)',
  }}
  bodyStyle={{ padding: 28 }}
>
  <Statistic
    title={<span style={{ color: '#64748B', fontSize: 13, fontWeight: 500 }}>Label</span>}
    value={value}
    prefix={<Icon style={{ color: '#14B8A6', fontSize: 20 }} />}
    valueStyle={{ color: '#1E293B', fontSize: 32, fontWeight: 700 }}
  />
</Card>
```

#### **Primary Button**
```typescript
<Button
  type="primary"
  icon={<Icon />}
  style={{
    borderRadius: 10,
    height: 38,
    background: 'linear-gradient(135deg, #10B981 0%, #14B8A6 100%)',
    boxShadow: '0 2px 6px rgba(16, 185, 129, 0.2)',
  }}
>
  Action
</Button>
```

#### **Grid Layout**
```typescript
<Row gutter={[20, 20]} style={{ marginBottom: 24 }}>
  <Col xs={24} sm={12} lg={6}>
    {/* Content */}
  </Col>
</Row>
```

---

## 🚀 Implementation Notes

### **File Structure**
- **CSS Variables**: `src/index.css` (root variables)
- **Ant Design Theme**: `src/App.tsx` (ConfigProvider)
- **Global Styles**: `src/App.css`
- **Component Styles**: Inline (for precision)

### **When to Update This Guide**
- Adding new color to the palette
- Changing spacing system
- Introducing new component pattern
- Modifying border radius values
- Updating typography scale
- Adding new animation pattern

### **Version History**
- **v1.0** - Initial Chrome-inspired design system
- **v1.1** - Reduced border radius for modern look
- **v1.2** - Simplified header, reorganized sidebar

---

**Remember**: Consistency is key. When in doubt, reference existing components and this guide. Every pixel matters! 🎯

