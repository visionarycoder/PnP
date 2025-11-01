# Accessibility Patterns

**Description**: Web accessibility patterns and WCAG compliance techniques
**Language/Technology**: HTML5, CSS3, JavaScript / Web Accessibility

## ARIA Patterns and Semantic HTML

**Code**:

```html
<!-- Accessible Navigation -->
<nav role="navigation" aria-label="Main navigation">
    <ul class="nav-menu">
        <li><a href="/" aria-current="page">Home</a></li>
        <li><a href="/products">Products</a></li>
        <li><a href="/about">About</a></li>
        <li><a href="/contact">Contact</a></li>
    </ul>
</nav>

<!-- Skip Navigation Link -->
<a class="skip-link" href="#main-content">Skip to main content</a>

<main id="main-content" tabindex="-1">
    <!-- Main content -->
</main>

<!-- Accessible Headings Structure -->
<h1>Page Title</h1>
<section aria-labelledby="section1-title">
    <h2 id="section1-title">Section Title</h2>
    <article aria-labelledby="article1-title">
        <h3 id="article1-title">Article Title</h3>
        <p>Article content...</p>
        <h4>Subsection</h4>
        <p>Subsection content...</p>
    </article>
</section>

<!-- Accessible Form with ARIA -->
<form class="contact-form" novalidate>
    <fieldset>
        <legend>Contact Information</legend>
        
        <div class="form-group">
            <label for="name">
                Full Name 
                <span class="required" aria-label="required">*</span>
            </label>
            <input 
                type="text" 
                id="name" 
                name="name" 
                required 
                aria-describedby="name-error name-help"
                aria-invalid="false"
                autocomplete="name"
            >
            <div id="name-help" class="help-text">
                Enter your first and last name
            </div>
            <div id="name-error" class="error-message" role="alert" aria-live="polite">
                <!-- Error message will appear here -->
            </div>
        </div>
        
        <div class="form-group">
            <label for="email">
                Email Address
                <span class="required" aria-label="required">*</span>
            </label>
            <input 
                type="email" 
                id="email" 
                name="email" 
                required 
                aria-describedby="email-error email-format"
                aria-invalid="false"
                autocomplete="email"
            >
            <div id="email-format" class="help-text">
                Format: name@example.com
            </div>
            <div id="email-error" class="error-message" role="alert" aria-live="polite"></div>
        </div>
        
        <div class="form-group">
            <fieldset class="radio-group">
                <legend>Preferred Contact Method</legend>
                <div class="radio-options">
                    <label>
                        <input type="radio" name="contact-method" value="email" checked>
                        Email
                    </label>
                    <label>
                        <input type="radio" name="contact-method" value="phone">
                        Phone
                    </label>
                    <label>
                        <input type="radio" name="contact-method" value="mail">
                        Postal Mail
                    </label>
                </div>
            </fieldset>
        </div>
        
        <div class="form-group">
            <label for="message">Message</label>
            <textarea 
                id="message" 
                name="message" 
                rows="5"
                aria-describedby="message-counter"
                placeholder="Enter your message here..."
            ></textarea>
            <div id="message-counter" class="character-counter" aria-live="polite">
                0 / 500 characters
            </div>
        </div>
    </fieldset>
    
    <div class="form-actions">
        <button type="submit" class="btn btn-primary">
            Send Message
        </button>
        <button type="reset" class="btn btn-secondary">
            Clear Form
        </button>
    </div>
</form>

<!-- Accessible Data Table -->
<table class="data-table" role="table" aria-label="Sales data by quarter">
    <caption>Quarterly Sales Report for 2024</caption>
    <thead>
        <tr>
            <th scope="col" id="quarter">Quarter</th>
            <th scope="col" id="revenue">Revenue</th>
            <th scope="col" id="units">Units Sold</th>
            <th scope="col" id="growth">Growth %</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <th scope="row" headers="quarter">Q1 2024</th>
            <td headers="quarter revenue">$125,000</td>
            <td headers="quarter units">1,250</td>
            <td headers="quarter growth">+15%</td>
        </tr>
        <tr>
            <th scope="row" headers="quarter">Q2 2024</th>
            <td headers="quarter revenue">$142,000</td>
            <td headers="quarter units">1,420</td>
            <td headers="quarter growth">+13.6%</td>
        </tr>
    </tbody>
</table>
```

## Interactive Components with Accessibility

**Code**:

```html
<!-- Accessible Modal Dialog -->
<div class="modal-overlay" id="modal-overlay" aria-hidden="true">
    <div 
        class="modal" 
        role="dialog" 
        aria-modal="true"
        aria-labelledby="modal-title" 
        aria-describedby="modal-description"
    >
        <div class="modal-header">
            <h2 id="modal-title">Confirmation Required</h2>
            <button 
                type="button" 
                class="modal-close" 
                aria-label="Close dialog"
                data-dismiss="modal"
            >
                <span aria-hidden="true">&times;</span>
            </button>
        </div>
        <div class="modal-body">
            <p id="modal-description">
                Are you sure you want to delete this item? This action cannot be undone.
            </p>
        </div>
        <div class="modal-footer">
            <button type="button" class="btn btn-secondary" data-dismiss="modal">
                Cancel
            </button>
            <button type="button" class="btn btn-danger" id="confirm-delete">
                Delete Item
            </button>
        </div>
    </div>
</div>

<!-- Accessible Accordion -->
<div class="accordion" id="faq-accordion">
    <div class="accordion-item">
        <h3 class="accordion-header">
            <button 
                class="accordion-button"
                type="button"
                aria-expanded="false"
                aria-controls="accordion-panel-1"
                id="accordion-header-1"
            >
                What is your return policy?
            </button>
        </h3>
        <div 
            class="accordion-panel" 
            id="accordion-panel-1"
            role="region"
            aria-labelledby="accordion-header-1"
            hidden
        >
            <div class="accordion-content">
                <p>We offer a 30-day return policy on all items...</p>
            </div>
        </div>
    </div>
    
    <div class="accordion-item">
        <h3 class="accordion-header">
            <button 
                class="accordion-button"
                type="button"
                aria-expanded="false"
                aria-controls="accordion-panel-2"
                id="accordion-header-2"
            >
                How do I track my order?
            </button>
        </h3>
        <div 
            class="accordion-panel" 
            id="accordion-panel-2"
            role="region"
            aria-labelledby="accordion-header-2"
            hidden
        >
            <div class="accordion-content">
                <p>You can track your order using the tracking number...</p>
            </div>
        </div>
    </div>
</div>

<!-- Accessible Tabs -->
<div class="tabs" role="tabpanel">
    <div class="tab-list" role="tablist" aria-label="Account settings">
        <button 
            class="tab" 
            role="tab" 
            aria-selected="true" 
            aria-controls="profile-panel" 
            id="profile-tab"
            tabindex="0"
        >
            Profile
        </button>
        <button 
            class="tab" 
            role="tab" 
            aria-selected="false" 
            aria-controls="security-panel" 
            id="security-tab"
            tabindex="-1"
        >
            Security
        </button>
        <button 
            class="tab" 
            role="tab" 
            aria-selected="false" 
            aria-controls="notifications-panel" 
            id="notifications-tab"
            tabindex="-1"
        >
            Notifications
        </button>
    </div>
    
    <div class="tab-panels">
        <div 
            class="tab-panel" 
            role="tabpanel" 
            id="profile-panel" 
            aria-labelledby="profile-tab"
            tabindex="0"
        >
            <h3>Profile Settings</h3>
            <p>Manage your profile information here.</p>
        </div>
        <div 
            class="tab-panel" 
            role="tabpanel" 
            id="security-panel" 
            aria-labelledby="security-tab"
            tabindex="0"
            hidden
        >
            <h3>Security Settings</h3>
            <p>Configure your security preferences.</p>
        </div>
        <div 
            class="tab-panel" 
            role="tabpanel" 
            id="notifications-panel" 
            aria-labelledby="notifications-tab"
            tabindex="0"
            hidden
        >
            <h3>Notification Settings</h3>
            <p>Choose how you want to be notified.</p>
        </div>
    </div>
</div>

<!-- Accessible Dropdown/Combobox -->
<div class="combobox-container">
    <label for="country-select">Select Country</label>
    <div class="combobox">
        <input 
            type="text" 
            id="country-select"
            role="combobox"
            aria-expanded="false"
            aria-haspopup="listbox"
            aria-autocomplete="list"
            aria-describedby="country-help"
            placeholder="Start typing to search..."
        >
        <button 
            type="button" 
            class="combobox-button"
            aria-label="Open country list"
            tabindex="-1"
        >
            ▼
        </button>
        <ul 
            class="combobox-options" 
            role="listbox"
            id="country-listbox"
            aria-label="Countries"
            hidden
        >
            <li role="option" id="option-us" data-value="US">United States</li>
            <li role="option" id="option-ca" data-value="CA">Canada</li>
            <li role="option" id="option-mx" data-value="MX">Mexico</li>
            <li role="option" id="option-uk" data-value="UK">United Kingdom</li>
        </ul>
    </div>
    <div id="country-help" class="help-text">
        Type to search or use arrow keys to browse options
    </div>
</div>
```

## CSS for Accessibility

**Code**:

```css
/* Skip Link Styles */
.skip-link {
    position: absolute;
    top: -40px;
    left: 6px;
    background: #000;
    color: #fff;
    padding: 8px;
    text-decoration: none;
    border-radius: 0 0 4px 4px;
    z-index: 1000;
    transition: top 0.3s;
}

.skip-link:focus {
    top: 0;
}

/* Focus Management */
*:focus {
    outline: 3px solid #4A90E2;
    outline-offset: 2px;
}

/* Custom focus for interactive elements */
.btn:focus,
.form-control:focus,
.tab:focus {
    outline: 3px solid #4A90E2;
    outline-offset: 2px;
    box-shadow: 0 0 0 1px #4A90E2;
}

/* Remove default outline only when adding custom styles */
.custom-focus:focus {
    outline: none;
    box-shadow: 
        0 0 0 2px #fff,
        0 0 0 4px #4A90E2;
}

/* High Contrast Mode Support */
@media (prefers-contrast: high) {
    .btn {
        border: 2px solid currentColor;
    }
    
    .card {
        border: 1px solid currentColor;
    }
}

/* Reduced Motion Support */
@media (prefers-reduced-motion: reduce) {
    *,
    *::before,
    *::after {
        animation-duration: 0.01ms !important;
        animation-iteration-count: 1 !important;
        transition-duration: 0.01ms !important;
        scroll-behavior: auto !important;
    }
}

/* Dark Mode Accessibility */
@media (prefers-color-scheme: dark) {
    :root {
        --text-color: #e0e0e0;
        --bg-color: #121212;
        --border-color: #333;
        --focus-color: #66B3FF;
    }
    
    body {
        background-color: var(--bg-color);
        color: var(--text-color);
    }
    
    .btn:focus {
        outline-color: var(--focus-color);
    }
}

/* Screen Reader Only Content */
.sr-only {
    position: absolute !important;
    width: 1px !important;
    height: 1px !important;
    padding: 0 !important;
    margin: -1px !important;
    overflow: hidden !important;
    clip: rect(0, 0, 0, 0) !important;
    white-space: nowrap !important;
    border: 0 !important;
}

/* Form Validation States */
.form-control[aria-invalid="true"] {
    border-color: #dc3545;
    box-shadow: 0 0 0 0.2rem rgba(220, 53, 69, 0.25);
}

.form-control[aria-invalid="false"] {
    border-color: #28a745;
}

.error-message {
    color: #dc3545;
    font-size: 0.875rem;
    margin-top: 0.25rem;
}

.error-message:empty {
    display: none;
}

/* Accessible Color Combinations */
:root {
    /* WCAG AA compliant color combinations */
    --primary-text: #212529;      /* 16.46:1 on white */
    --secondary-text: #6c757d;    /* 4.52:1 on white */
    --success-text: #155724;      /* 7.24:1 on white */
    --warning-text: #856404;      /* 5.45:1 on white */
    --error-text: #721c24;        /* 8.11:1 on white */
    
    --primary-bg: #007bff;
    --success-bg: #28a745;
    --warning-bg: #ffc107;
    --error-bg: #dc3545;
}

/* Touch Target Sizing */
.btn,
.form-control,
.tab,
.accordion-button {
    min-height: 44px;  /* iOS minimum touch target */
    min-width: 44px;
    padding: 12px 16px;
}

@media (hover: hover) {
    .btn:hover {
        /* Hover states only for devices that support hover */
        transform: translateY(-1px);
    }
}

/* Responsive Typography for Accessibility */
body {
    font-size: clamp(16px, 2.5vw, 18px);
    line-height: 1.6;
}

h1, h2, h3, h4, h5, h6 {
    line-height: 1.3;
    margin-bottom: 0.5em;
}

/* Links and Interactive Elements */
a {
    color: #0066cc;
    text-decoration: underline;
}

a:hover,
a:focus {
    color: #004499;
    text-decoration: none;
    background-color: rgba(0, 102, 204, 0.1);
}

/* Visited links with sufficient contrast */
a:visited {
    color: #551a8b;
}

/* Table Accessibility */
.data-table {
    border-collapse: collapse;
    width: 100%;
}

.data-table th,
.data-table td {
    border: 1px solid #ddd;
    padding: 12px;
    text-align: left;
}

.data-table th {
    background-color: #f8f9fa;
    font-weight: bold;
}

.data-table caption {
    caption-side: top;
    padding: 10px;
    font-weight: bold;
    text-align: left;
}
```

## JavaScript Accessibility Helpers

**Code**:

```javascript
// Focus Management
class FocusManager {
    static trapFocus(element) {
        const focusableElements = element.querySelectorAll(
            'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
        );
        const firstElement = focusableElements[0];
        const lastElement = focusableElements[focusableElements.length - 1];
        
        element.addEventListener('keydown', (e) => {
            if (e.key === 'Tab') {
                if (e.shiftKey) {
                    if (document.activeElement === firstElement) {
                        e.preventDefault();
                        lastElement.focus();
                    }
                } else {
                    if (document.activeElement === lastElement) {
                        e.preventDefault();
                        firstElement.focus();
                    }
                }
            }
        });
    }
    
    static returnFocus(element) {
        if (element && typeof element.focus === 'function') {
            element.focus();
        }
    }
}

// ARIA Live Region Manager
class LiveRegionManager {
    constructor() {
        this.politeRegion = this.createLiveRegion('polite');
        this.assertiveRegion = this.createLiveRegion('assertive');
    }
    
    createLiveRegion(politeness) {
        const region = document.createElement('div');
        region.setAttribute('aria-live', politeness);
        region.setAttribute('aria-atomic', 'true');
        region.className = 'sr-only';
        document.body.appendChild(region);
        return region;
    }
    
    announce(message, priority = 'polite') {
        const region = priority === 'assertive' ? 
            this.assertiveRegion : this.politeRegion;
        
        region.textContent = '';
        setTimeout(() => {
            region.textContent = message;
        }, 100);
    }
}

// Accessible Modal
class AccessibleModal {
    constructor(modalId, triggerId) {
        this.modal = document.getElementById(modalId);
        this.trigger = document.getElementById(triggerId);
        this.lastFocused = null;
        this.init();
    }
    
    init() {
        this.trigger.addEventListener('click', () => this.open());
        this.modal.addEventListener('click', (e) => {
            if (e.target === this.modal) this.close();
        });
        
        // Close on Escape key
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && this.isOpen()) {
                this.close();
            }
        });
        
        // Close buttons
        this.modal.querySelectorAll('[data-dismiss="modal"]')
            .forEach(btn => btn.addEventListener('click', () => this.close()));
    }
    
    open() {
        this.lastFocused = document.activeElement;
        this.modal.setAttribute('aria-hidden', 'false');
        this.modal.style.display = 'flex';
        
        // Focus first focusable element
        const firstFocusable = this.modal.querySelector(
            'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
        );
        if (firstFocusable) {
            firstFocusable.focus();
        }
        
        // Trap focus
        FocusManager.trapFocus(this.modal);
        
        // Prevent body scroll
        document.body.style.overflow = 'hidden';
    }
    
    close() {
        this.modal.setAttribute('aria-hidden', 'true');
        this.modal.style.display = 'none';
        
        // Restore focus
        if (this.lastFocused) {
            this.lastFocused.focus();
        }
        
        // Restore body scroll
        document.body.style.overflow = '';
    }
    
    isOpen() {
        return this.modal.getAttribute('aria-hidden') === 'false';
    }
}

// Accessible Tabs
class AccessibleTabs {
    constructor(tabsContainer) {
        this.container = tabsContainer;
        this.tabList = this.container.querySelector('[role="tablist"]');
        this.tabs = Array.from(this.tabList.querySelectorAll('[role="tab"]'));
        this.panels = Array.from(this.container.querySelectorAll('[role="tabpanel"]'));
        this.init();
    }
    
    init() {
        this.tabs.forEach((tab, index) => {
            tab.addEventListener('click', () => this.activateTab(index));
            tab.addEventListener('keydown', (e) => this.handleKeyDown(e, index));
        });
    }
    
    activateTab(index) {
        // Deactivate all tabs and panels
        this.tabs.forEach(tab => {
            tab.setAttribute('aria-selected', 'false');
            tab.setAttribute('tabindex', '-1');
        });
        
        this.panels.forEach(panel => {
            panel.hidden = true;
        });
        
        // Activate selected tab and panel
        const activeTab = this.tabs[index];
        const activePanel = this.panels[index];
        
        activeTab.setAttribute('aria-selected', 'true');
        activeTab.setAttribute('tabindex', '0');
        activeTab.focus();
        
        activePanel.hidden = false;
        
        // Announce tab change
        const liveRegion = new LiveRegionManager();
        liveRegion.announce(`${activeTab.textContent} tab selected`);
    }
    
    handleKeyDown(event, currentIndex) {
        let targetIndex = currentIndex;
        
        switch (event.key) {
            case 'ArrowRight':
                targetIndex = (currentIndex + 1) % this.tabs.length;
                break;
            case 'ArrowLeft':
                targetIndex = currentIndex === 0 ? 
                    this.tabs.length - 1 : currentIndex - 1;
                break;
            case 'Home':
                targetIndex = 0;
                break;
            case 'End':
                targetIndex = this.tabs.length - 1;
                break;
            default:
                return;
        }
        
        event.preventDefault();
        this.activateTab(targetIndex);
    }
}

// Form Validation with Accessibility
class AccessibleFormValidator {
    constructor(form) {
        this.form = form;
        this.liveRegion = new LiveRegionManager();
        this.init();
    }
    
    init() {
        this.form.addEventListener('submit', (e) => this.validateForm(e));
        
        // Real-time validation
        this.form.querySelectorAll('input, textarea, select').forEach(field => {
            field.addEventListener('blur', () => this.validateField(field));
            field.addEventListener('input', () => this.clearFieldError(field));
        });
    }
    
    validateForm(event) {
        const errors = [];
        const fields = this.form.querySelectorAll('[required]');
        
        fields.forEach(field => {
            if (!this.validateField(field)) {
                errors.push(field);
            }
        });
        
        if (errors.length > 0) {
            event.preventDefault();
            
            // Focus first error field
            errors[0].focus();
            
            // Announce errors
            this.liveRegion.announce(
                `Form has ${errors.length} error${errors.length === 1 ? '' : 's'}. Please correct and try again.`,
                'assertive'
            );
        }
    }
    
    validateField(field) {
        const errorElement = document.getElementById(
            field.getAttribute('aria-describedby')?.split(' ')
                .find(id => id.includes('error')) || ''
        );
        
        let isValid = true;
        let errorMessage = '';
        
        if (field.hasAttribute('required') && !field.value.trim()) {
            isValid = false;
            errorMessage = 'This field is required.';
        } else if (field.type === 'email' && field.value && 
                  !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(field.value)) {
            isValid = false;
            errorMessage = 'Please enter a valid email address.';
        }
        
        field.setAttribute('aria-invalid', isValid ? 'false' : 'true');
        
        if (errorElement) {
            errorElement.textContent = errorMessage;
        }
        
        return isValid;
    }
    
    clearFieldError(field) {
        const errorElement = document.getElementById(
            field.getAttribute('aria-describedby')?.split(' ')
                .find(id => id.includes('error')) || ''
        );
        
        if (errorElement && field.value.trim()) {
            errorElement.textContent = '';
            field.setAttribute('aria-invalid', 'false');
        }
    }
}

// Initialize components
document.addEventListener('DOMContentLoaded', () => {
    // Initialize modals
    document.querySelectorAll('[data-toggle="modal"]').forEach(trigger => {
        const modalId = trigger.getAttribute('data-target');
        new AccessibleModal(modalId.substring(1), trigger.id);
    });
    
    // Initialize tabs
    document.querySelectorAll('.tabs').forEach(tabContainer => {
        new AccessibleTabs(tabContainer);
    });
    
    // Initialize form validation
    document.querySelectorAll('form[novalidate]').forEach(form => {
        new AccessibleFormValidator(form);
    });
});
```

**Usage**:

```html
<!-- Use semantic HTML first -->
<nav aria-label="Main navigation">
    <ul>
        <li><a href="/" aria-current="page">Home</a></li>
        <li><a href="/products">Products</a></li>
    </ul>
</nav>

<!-- Add ARIA when semantics aren't enough -->
<div role="button" tabindex="0" aria-pressed="false">
    Custom Button
</div>

<!-- Provide text alternatives -->
<img src="chart.png" alt="Sales increased 25% from Q1 to Q2">

<!-- Use proper form labels -->
<label for="email">Email Address</label>
<input type="email" id="email" required aria-describedby="email-help">
<div id="email-help">We'll never share your email</div>
```

**Notes**:

- **Testing**: Use screen readers (NVDA, JAWS, VoiceOver) for testing
- **Standards**: Follow WCAG 2.1 AA guidelines at minimum
- **Color**: Ensure 4.5:1 contrast ratio for normal text, 3:1 for large text
- **Keyboard**: All functionality must be keyboard accessible
- **Focus**: Provide clear focus indicators for all interactive elements
- **Alternative Text**: Write descriptive alt text for images
- **Semantic HTML**: Use proper HTML elements before adding ARIA
- **Progressive Enhancement**: Ensure basic functionality without JavaScript

## Related Snippets

- [HTML Templates](html-templates.md) - Semantic HTML structures
- [Responsive Design](responsive-design.md) - Accessible responsive patterns
- [Form Validation](../javascript/form-validation.md) - Accessible form handling
