# CSS Layouts

**Description**: Modern CSS layout techniques and patterns for responsive web design
**Language/Technology**: CSS3 / Web Development

## CSS Grid Layouts

**Code**:

```css
/* Basic Grid Container */
.grid-container {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
    grid-gap: 2rem;
    padding: 2rem;
}

/* Holy Grail Layout with CSS Grid */
.holy-grail {
    display: grid;
    min-height: 100vh;
    grid-template-areas:
        "header header header"
        "sidebar main aside"
        "footer footer footer";
    grid-template-rows: auto 1fr auto;
    grid-template-columns: 200px 1fr 150px;
}

.header { 
    grid-area: header; 
    background: #2c3e50;
    color: white;
    padding: 1rem;
}

.sidebar { 
    grid-area: sidebar; 
    background: #ecf0f1;
    padding: 1rem;
}

.main { 
    grid-area: main; 
    padding: 2rem;
}

.aside { 
    grid-area: aside; 
    background: #bdc3c7;
    padding: 1rem;
}

.footer { 
    grid-area: footer; 
    background: #34495e;
    color: white;
    padding: 1rem;
    text-align: center;
}

/* Responsive Grid Layout */
@media (max-width: 768px) {
    .holy-grail {
        grid-template-areas:
            "header"
            "main"
            "sidebar"
            "aside"
            "footer";
        grid-template-columns: 1fr;
    }
}

/* Card Grid Layout */
.card-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
    grid-gap: 1.5rem;
    padding: 1rem;
}

.card {
    background: white;
    border-radius: 8px;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
    overflow: hidden;
    transition: transform 0.2s ease;
}

.card:hover {
    transform: translateY(-5px);
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
}

.card-header {
    padding: 1.5rem;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
}

.card-body {
    padding: 1.5rem;
}

.card-footer {
    padding: 1rem 1.5rem;
    background: #f8f9fa;
    border-top: 1px solid #e9ecef;
}

/* Masonry Layout with CSS Grid */
.masonry {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
    grid-auto-rows: 10px;
    grid-gap: 1rem;
}

.masonry-item {
    background: white;
    border-radius: 8px;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    overflow: hidden;
}

/* JavaScript will calculate grid-row-end based on content height */
.masonry-item.span-2 { grid-row-end: span 20; }
.masonry-item.span-3 { grid-row-end: span 30; }
.masonry-item.span-4 { grid-row-end: span 40; }
```

## Flexbox Layouts

**Code**:

```css
/* Flexbox Container Basics */
.flex-container {
    display: flex;
    flex-direction: row;
    flex-wrap: wrap;
    justify-content: space-between;
    align-items: center;
    gap: 1rem;
}

/* Navigation Bar with Flexbox */
.navbar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem 2rem;
    background: white;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.navbar-brand {
    font-size: 1.5rem;
    font-weight: bold;
    color: #2c3e50;
}

.navbar-menu {
    display: flex;
    list-style: none;
    margin: 0;
    padding: 0;
    gap: 2rem;
}

.navbar-menu a {
    text-decoration: none;
    color: #34495e;
    font-weight: 500;
    transition: color 0.2s ease;
}

.navbar-menu a:hover {
    color: #3498db;
}

.navbar-actions {
    display: flex;
    gap: 1rem;
    align-items: center;
}

/* Responsive Navigation */
@media (max-width: 768px) {
    .navbar {
        flex-direction: column;
        align-items: stretch;
    }
    
    .navbar-menu {
        flex-direction: column;
        gap: 1rem;
        margin-top: 1rem;
    }
    
    .navbar-actions {
        justify-content: center;
        margin-top: 1rem;
    }
}

/* Flexible Card Layout */
.flex-cards {
    display: flex;
    flex-wrap: wrap;
    gap: 1.5rem;
    padding: 1rem;
}

.flex-card {
    flex: 1 1 300px; /* grow, shrink, basis */
    min-height: 200px;
    background: white;
    border-radius: 8px;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    display: flex;
    flex-direction: column;
}

.flex-card-content {
    padding: 1.5rem;
    flex: 1; /* Takes remaining space */
}

.flex-card-actions {
    padding: 1rem 1.5rem;
    border-top: 1px solid #e9ecef;
    margin-top: auto; /* Push to bottom */
}

/* Center Content Layout */
.center-layout {
    display: flex;
    min-height: 100vh;
    justify-content: center;
    align-items: center;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.center-content {
    background: white;
    padding: 3rem;
    border-radius: 12px;
    box-shadow: 0 10px 30px rgba(0, 0, 0, 0.2);
    max-width: 500px;
    width: 90%;
}

/* Sidebar Layout with Flexbox */
.sidebar-layout {
    display: flex;
    min-height: 100vh;
}

.sidebar {
    flex: 0 0 250px; /* Don't grow/shrink, fixed width */
    background: #2c3e50;
    color: white;
    padding: 2rem 1rem;
    overflow-y: auto;
}

.main-content {
    flex: 1; /* Take remaining space */
    padding: 2rem;
    overflow-y: auto;
}

@media (max-width: 768px) {
    .sidebar-layout {
        flex-direction: column;
    }
    
    .sidebar {
        flex: 0 0 auto;
        order: 2; /* Move to bottom on mobile */
    }
    
    .main-content {
        order: 1;
    }
}
```

## Modern Layout Patterns

**Code**:

```css
/* Container Queries (Modern Browsers) */
.component {
    container-type: inline-size;
}

@container (min-width: 400px) {
    .component-content {
        display: flex;
        gap: 1rem;
    }
}

@container (min-width: 600px) {
    .component-content {
        flex-direction: row;
    }
}

/* CSS Subgrid (Modern Browsers) */
.parent-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 1rem;
}

.child-grid {
    display: grid;
    grid-column: span 2;
    grid-template-columns: subgrid; /* Inherits parent grid */
    gap: inherit;
}

/* Aspect Ratio Layout */
.aspect-ratio-container {
    aspect-ratio: 16 / 9;
    background: #f0f0f0;
    border-radius: 8px;
    overflow: hidden;
}

.aspect-ratio-container img,
.aspect-ratio-container video {
    width: 100%;
    height: 100%;
    object-fit: cover;
}

/* Sticky Header Layout */
.sticky-header {
    position: sticky;
    top: 0;
    background: white;
    border-bottom: 1px solid #e0e0e0;
    z-index: 100;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

/* Split Screen Layout */
.split-screen {
    display: grid;
    grid-template-columns: 1fr 1fr;
    min-height: 100vh;
}

.split-left {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    display: flex;
    align-items: center;
    justify-content: center;
    color: white;
    padding: 2rem;
}

.split-right {
    background: white;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 2rem;
}

@media (max-width: 768px) {
    .split-screen {
        grid-template-columns: 1fr;
        grid-template-rows: 1fr 1fr;
    }
}

/* Magazine Layout */
.magazine-layout {
    display: grid;
    grid-template-columns: repeat(12, 1fr);
    grid-auto-rows: 100px;
    gap: 1rem;
    padding: 1rem;
}

.feature-article {
    grid-column: span 8;
    grid-row: span 4;
    background: #2c3e50;
    color: white;
    border-radius: 8px;
    padding: 2rem;
    display: flex;
    flex-direction: column;
    justify-content: end;
}

.sidebar-article {
    grid-column: span 4;
    grid-row: span 2;
    background: white;
    border: 1px solid #e0e0e0;
    border-radius: 8px;
    padding: 1rem;
}

.small-article {
    grid-column: span 3;
    grid-row: span 2;
    background: white;
    border: 1px solid #e0e0e0;
    border-radius: 8px;
    padding: 1rem;
}

@media (max-width: 768px) {
    .magazine-layout {
        grid-template-columns: 1fr;
    }
    
    .feature-article,
    .sidebar-article,
    .small-article {
        grid-column: span 1;
    }
}

/* Pancake Layout (Header, Content, Footer) */
.pancake-layout {
    display: flex;
    flex-direction: column;
    min-height: 100vh;
}

.pancake-header,
.pancake-footer {
    flex: 0 0 auto;
}

.pancake-main {
    flex: 1 0 auto;
    display: flex;
    flex-direction: column;
}

/* Pancake Stack (Even Distribution) */
.pancake-stack {
    display: flex;
    flex-direction: column;
    justify-content: center;
    min-height: 100vh;
    padding: 2rem;
}

.pancake-stack > * {
    flex: 0 1 auto;
    margin: 1rem 0;
}
```

## Responsive Design Patterns

**Code**:

```css
/* Mobile-First Approach */
.responsive-container {
    width: 100%;
    max-width: 1200px;
    margin: 0 auto;
    padding: 0 1rem;
}

/* Breakpoint System */
@media (min-width: 576px) {
    .responsive-container {
        padding: 0 1.5rem;
    }
}

@media (min-width: 768px) {
    .responsive-container {
        padding: 0 2rem;
    }
}

@media (min-width: 992px) {
    .responsive-container {
        padding: 0 2.5rem;
    }
}

@media (min-width: 1200px) {
    .responsive-container {
        padding: 0 3rem;
    }
}

/* Fluid Typography */
.fluid-text {
    font-size: clamp(1rem, 2.5vw, 2rem);
    line-height: 1.6;
}

.fluid-heading {
    font-size: clamp(1.5rem, 5vw, 4rem);
    line-height: 1.2;
}

/* Intrinsic Web Design */
.intrinsic-layout {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(min(300px, 100%), 1fr));
    gap: 1rem;
}

/* Component-Based Responsive Design */
.responsive-component {
    --columns: 1;
    display: grid;
    grid-template-columns: repeat(var(--columns), 1fr);
    gap: 1rem;
}

@media (min-width: 768px) {
    .responsive-component {
        --columns: 2;
    }
}

@media (min-width: 1024px) {
    .responsive-component {
        --columns: 3;
    }
}

@media (min-width: 1400px) {
    .responsive-component {
        --columns: 4;
    }
}
```

**Usage**:

```css
/* Grid Layout Example */
.product-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
    gap: 2rem;
    padding: 2rem;
}

/* Flexbox Navigation Example */
.main-nav {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem 2rem;
}

/* Responsive Card Layout */
.card-container {
    display: flex;
    flex-wrap: wrap;
    gap: 1.5rem;
    padding: 1rem;
}

.card {
    flex: 1 1 calc(33.333% - 1rem);
    min-width: 300px;
}

@media (max-width: 768px) {
    .card {
        flex: 1 1 100%;
    }
}
```

**Notes**:

- **Performance**: Use CSS containment for large layouts
- **Accessibility**: Maintain logical tab order and focus management
- **Browser Support**: Check compatibility for modern CSS features
- **Mobile**: Test layouts on actual devices, not just browser dev tools
- **Print Styles**: Consider print-friendly layouts
- **Dark Mode**: Implement theme-aware layouts using CSS custom properties
- **Internationalization**: Account for different text lengths and directions
- **Progressive Enhancement**: Ensure basic layout works without advanced CSS features

## Related Snippets

- [Responsive Design](responsive-design.md) - Mobile-first design patterns
- [CSS Grid](css-grid.md) - Advanced grid techniques
- [Flexbox](flexbox.md) - Flexible layout patterns
