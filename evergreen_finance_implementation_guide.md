# Design System Specification: Evergreen Finance

## Overview
A minimalist and professional financial control interface focusing on clarity, trust, and growth.

## Design Tokens

### Color Palette (Evergreen Minimalist)
- **Primary:** `#064E3B` (Emerald 900) - Used for brand elements, primary buttons, and positive growth indicators.
- **Surface:** `#F8FAF6` - Main background color for a clean, airy feel.
- **Surface Container:** `#FFFFFF` - Used for cards and elevated elements.
- **Text Primary:** `#111827` (Slate 900) - For high-contrast headings and body text.
- **Text Secondary:** `#64748B` (Slate 500) - For labels and secondary information.
- **Accent/Positive:** `#10B981` (Emerald 500) - For positive financial trends.
- **Accent/Negative:** `#EF4444` (Red 500) - For expenses or negative alerts.

### Typography
- **Primary Font:** `Manrope` (Sans-serif)
- **Scale:**
  - **Headings:** Bold, tight tracking (-0.025em), Emerald 900.
  - **Body:** Regular/Medium, Slate 900.
  - **Labels/Small:** Medium, uppercase for table headers, Slate 500.

### UI Principles
- **Roundness:** `ROUND_FOUR` (approx. 8px - 12px) for cards and buttons to balance professional structure with modern softness.
- **Shadows:** Flat design with very subtle, soft elevations (`0 1px 3px rgba(0,0,0,0.05)`) to maintain minimalism.
- **Spacing:** Generous white space (8px/16px/24px/32px grid) to reduce cognitive load.

## Shared Components

### 1. Top Navigation Bar
- Minimalist height (64px).
- Search bar for transactions.
- Profile trigger and notification icons.
- Border-bottom: 1px solid Slate 200.

### 2. Side Navigation
- Fixed width (256px).
- Active state: Light emerald background with a dark emerald left/right border.
- Bottom section for "Help Center" and "Sign Out".

### 3. Action Buttons
- **Primary:** Solid Emerald 900, white text, rounded corners.
- **Secondary:** Outlined or ghost buttons with Emerald 900 text.

## Screen Architectures

### 1. Login & Registration
- Centered card layout.
- Tabbed interface (Login / Create Account).
- Social auth integration (Google/SSO).

### 2. Dashboard (Home)
- **Header:** Large "Portfolio Overview" title.
- **Summary Cards:** Three-column grid (Total Balance, Monthly Income, Monthly Expenses).
- **Transactions Grid:** Detailed table with columns for Date, Description, Category, Value, and Status.
- **Visual Insights:** Mini bar charts and progress bars for savings goals.

### 3. User Profile
- Split view: Sidebar with user avatar/summary and main area for forms.
- Form Sections: Personal Information, Security & Password, Notifications.
- Danger Zone: "Deactivate Account" section with distinct styling.