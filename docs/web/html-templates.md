# Modern HTML5 Templates

**Description**: Enterprise-grade HTML5 templates with WCAG 2.1 AA accessibility compliance, SEO optimization, Progressive Web App features, and performance-first architecture
**Language/Technology**: HTML5 / Modern Web Standards

## Progressive Web App Template

**Code**:

```html
<!DOCTYPE html>
<html lang="en" data-theme="light">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0, viewport-fit=cover">
    
    <!-- Primary Meta Tags -->
    <title>Enterprise Web Application | Your Company</title>
    <meta name="title" content="Enterprise Web Application | Your Company">
    <meta name="description" content="High-performance progressive web application with offline capabilities and modern user experience">
    
    <!-- SEO & Accessibility -->
    <meta name="robots" content="index, follow">
    <meta name="language" content="English">
    <meta name="author" content="Your Company">
    <link rel="canonical" href="https://yourcompany.com/">
    
    <!-- Open Graph / Facebook -->
    <meta property="og:type" content="website">
    <meta property="og:url" content="https://yourcompany.com/">
    <meta property="og:title" content="Enterprise Web Application | Your Company">
    <meta property="og:description" content="High-performance progressive web application with offline capabilities">
    <meta property="og:image" content="https://yourcompany.com/assets/og-image.jpg">
    <meta property="og:image:alt" content="Enterprise application interface preview">
    <meta property="og:site_name" content="Your Company">
    
    <!-- Twitter -->
    <meta property="twitter:card" content="summary_large_image">
    <meta property="twitter:url" content="https://yourcompany.com/">
    <meta property="twitter:title" content="Enterprise Web Application">
    <meta property="twitter:description" content="High-performance progressive web application">
    <meta property="twitter:image" content="https://yourcompany.com/assets/twitter-image.jpg">
    <meta property="twitter:image:alt" content="Application interface preview">
    
    <!-- PWA Manifest -->
    <link rel="manifest" href="/manifest.json">
    
    <!-- Security Headers -->
    <meta http-equiv="Content-Security-Policy" content="default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self' https:;">
    <meta http-equiv="Strict-Transport-Security" content="max-age=31536000; includeSubDomains">
    <meta http-equiv="X-Content-Type-Options" content="nosniff">
    <meta http-equiv="X-Frame-Options" content="DENY">
    <meta http-equiv="Referrer-Policy" content="strict-origin-when-cross-origin">
    
    <!-- Icons & Theme -->
    <link rel="icon" type="image/svg+xml" href="/assets/favicon.svg">
    <link rel="icon" type="image/png" sizes="32x32" href="/assets/favicon-32x32.png">
    <link rel="icon" type="image/png" sizes="16x16" href="/assets/favicon-16x16.png">
    <link rel="apple-touch-icon" sizes="180x180" href="/assets/apple-touch-icon.png">
    <link rel="mask-icon" href="/assets/safari-pinned-tab.svg" color="#1a1a1a">
    
    <!-- Theme Colors -->
    <meta name="theme-color" content="#1a1a1a" media="(prefers-color-scheme: light)">
    <meta name="theme-color" content="#ffffff" media="(prefers-color-scheme: dark)">
    <meta name="msapplication-TileColor" content="#1a1a1a">
    <meta name="msapplication-config" content="/assets/browserconfig.xml">
    
    <!-- Critical CSS (inline for performance) -->
    <style>
        /* Critical CSS for above-the-fold content */
        :root {
            --primary-color: #1a1a1a;
            --background-color: #ffffff;
            --text-color: #1a1a1a;
            --font-family: system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
        }
        
        @media (prefers-color-scheme: dark) {
            :root {
                --primary-color: #ffffff;
                --background-color: #1a1a1a;
                --text-color: #ffffff;
            }
        }
        
        * { box-sizing: border-box; }
        body {
            margin: 0;
            font-family: var(--font-family);
            background-color: var(--background-color);
            color: var(--text-color);
            line-height: 1.6;
        }
        
        .skip-link {
            position: absolute;
            top: -40px;
            left: 6px;
            background: var(--primary-color);
            color: var(--background-color);
            padding: 8px;
            text-decoration: none;
            z-index: 1000;
        }
        
        .skip-link:focus {
            top: 6px;
        }
    </style>
    
    <!-- Resource Hints -->
    <link rel="preconnect" href="https://fonts.googleapis.com" crossorigin>
    <link rel="preload" href="/assets/fonts/main.woff2" as="font" type="font/font-woff2" crossorigin>
    <link rel="prefetch" href="/api/user-preferences">
    
    <!-- Non-critical CSS -->
    <link rel="stylesheet" href="/assets/css/main.css" media="print" onload="this.media='all'">
    <noscript><link rel="stylesheet" href="/assets/css/main.css"></noscript>
    
    <!-- Structured Data -->
    <script type="application/ld+json">
    {
        "@context": "https://schema.org",
        "@type": "WebApplication",
        "name": "Enterprise Web Application",
        "description": "High-performance progressive web application",
        "url": "https://yourcompany.com",
        "applicationCategory": "BusinessApplication",
        "operatingSystem": "Any",
        "browserRequirements": "Requires modern web browser",
        "author": {
            "@type": "Organization",
            "name": "Your Company"
        }
    }
    </script>
</head>
<body>
    <!-- Skip Navigation Link -->
    <a href="#main-content" class="skip-link">Skip to main content</a>
    
    <!-- App Shell -->
    <div id="app" role="application" aria-label="Enterprise Web Application">
        <!-- Loading State -->
        <div id="loading-state" aria-live="polite" aria-label="Loading application">
            <div class="loading-spinner" role="status" aria-label="Loading..."></div>
        </div>
        
        <!-- Header with Navigation -->
        <header role="banner" class="app-header">
            <nav role="navigation" aria-label="Main navigation" class="main-nav">
                <div class="nav-container">
                    <!-- Logo and Brand -->
                    <div class="brand">
                        <a href="/" aria-label="Home - Your Company">
                            <img src="/assets/logo.svg" alt="Company Logo" width="120" height="40">
                        </a>
                    </div>
                    
                    <!-- Mobile Menu Toggle -->
                    <button 
                        class="mobile-menu-toggle" 
                        aria-expanded="false" 
                        aria-controls="main-menu"
                        aria-label="Toggle navigation menu">
                        <span class="hamburger-line"></span>
                        <span class="hamburger-line"></span>
                        <span class="hamburger-line"></span>
                    </button>
                    
                    <!-- Main Menu -->
                    <div id="main-menu" class="main-menu">
                        <ul role="menubar" class="nav-list">
                            <li role="none">
                                <a href="/dashboard" role="menuitem" aria-current="page">Dashboard</a>
                            </li>
                            <li role="none">
                                <a href="/projects" role="menuitem">Projects</a>
                            </li>
                            <li role="none">
                                <a href="/analytics" role="menuitem">Analytics</a>
                            </li>
                            <li role="none">
                                <a href="/settings" role="menuitem">Settings</a>
                            </li>
                        </ul>
                        
                        <!-- User Menu -->
                        <div class="user-menu">
                            <button 
                                class="user-menu-toggle" 
                                aria-expanded="false"
                                aria-haspopup="true"
                                aria-controls="user-dropdown">
                                <img src="/assets/user-avatar.jpg" alt="User avatar" class="user-avatar">
                                <span class="sr-only">User menu</span>
                            </button>
                            
                            <div id="user-dropdown" class="dropdown-menu" role="menu">
                                <a href="/profile" role="menuitem">Profile</a>
                                <a href="/preferences" role="menuitem">Preferences</a>
                                <button type="button" role="menuitem">Sign Out</button>
                            </div>
                        </div>
                    </div>
                </div>
            </nav>
        </header>
        
        <!-- Main Content Area -->
        <main id="main-content" role="main" class="main-content">
            <!-- Breadcrumb Navigation -->
            <nav aria-label="Breadcrumb" class="breadcrumb-nav">
                <ol class="breadcrumb-list">
                    <li><a href="/">Home</a></li>
                    <li aria-current="page">Dashboard</li>
                </ol>
            </nav>
            
            <!-- Page Content -->
            <div class="page-content">
                <!-- Content will be loaded dynamically -->
                <div id="dynamic-content" role="region" aria-live="polite" aria-label="Main content area">
                    <!-- Progressive enhancement: works without JavaScript -->
                    <noscript>
                        <div class="no-js-message" role="alert">
                            <h2>JavaScript Required</h2>
                            <p>This application requires JavaScript for full functionality. Please enable JavaScript in your browser.</p>
                        </div>
                    </noscript>
                </div>
            </div>
        </main>
        
        <!-- Footer -->
        <footer role="contentinfo" class="app-footer">
            <div class="footer-content">
                <div class="footer-section">
                    <h3>Company</h3>
                    <ul>
                        <li><a href="/about">About Us</a></li>
                        <li><a href="/contact">Contact</a></li>
                        <li><a href="/careers">Careers</a></li>
                    </ul>
                </div>
                
                <div class="footer-section">
                    <h3>Legal</h3>
                    <ul>
                        <li><a href="/privacy">Privacy Policy</a></li>
                        <li><a href="/terms">Terms of Service</a></li>
                        <li><a href="/cookies">Cookie Policy</a></li>
                    </ul>
                </div>
                
                <div class="footer-section">
                    <p>&copy; 2024 Your Company. All rights reserved.</p>
                </div>
            </div>
        </footer>
    </div>
    
    <!-- Accessibility Announcements -->
    <div id="announcements" aria-live="assertive" aria-atomic="true" class="sr-only"></div>
    
    <!-- Service Worker Registration -->
    <script>
        // Register service worker for offline functionality
        if ('serviceWorker' in navigator) {
            window.addEventListener('load', () => {
                navigator.serviceWorker.register('/sw.js')
                    .then(registration => console.log('SW registered'))
                    .catch(error => console.log('SW registration failed'));
            });
        }
    </script>
    
    <!-- Modern Module Scripts -->
    <script type="module" src="/assets/js/main.js"></script>
    <script nomodule src="/assets/js/main.legacy.js"></script>
</body>
</html>
```

## WCAG 2.1 AA Accessible Forms

**Code**:

```html
<!-- Comprehensive Accessible Contact Form -->
<form 
    class="contact-form" 
    method="post" 
    action="/contact" 
    novalidate
    aria-labelledby="form-title">
    
    <fieldset>
        <legend id="form-title" class="form-title">Contact Information</legend>
        
        <!-- Name Field with Error Handling -->
        <div class="field-group">
            <label for="full-name" class="field-label">
                Full Name
                <span aria-label="required" class="required-indicator">*</span>
            </label>
            <input 
                type="text" 
                id="full-name" 
                name="fullName"
                class="field-input"
                required
                aria-describedby="name-help name-error"
                aria-invalid="false"
                autocomplete="name"
                minlength="2"
                maxlength="100">
            
            <div id="name-help" class="field-help">
                Enter your first and last name
            </div>
            <div id="name-error" class="field-error" role="alert" aria-live="polite">
                <!-- Error message populated by JavaScript -->
            </div>
        </div>
        
        <!-- Email Field with Pattern Validation -->
        <div class="field-group">
            <label for="email-address" class="field-label">
                Email Address
                <span aria-label="required" class="required-indicator">*</span>
            </label>
            <input 
                type="email" 
                id="email-address" 
                name="email"
                class="field-input"
                required
                aria-describedby="email-help email-error"
                aria-invalid="false"
                autocomplete="email"
                pattern="[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}$">
            
            <div id="email-help" class="field-help">
                We'll use this to respond to your message
            </div>
            <div id="email-error" class="field-error" role="alert" aria-live="polite">
            </div>
        </div>
        
        <!-- Phone Field with Input Masking -->
        <div class="field-group">
            <label for="phone-number" class="field-label">
                Phone Number (Optional)
            </label>
            <input 
                type="tel" 
                id="phone-number" 
                name="phone"
                class="field-input"
                aria-describedby="phone-help phone-error"
                autocomplete="tel"
                pattern="[\+]?[0-9\s\-\(\)]{10,15}"
                placeholder="+1 (555) 123-4567">
            
            <div id="phone-help" class="field-help">
                Include country code for international numbers
            </div>
            <div id="phone-error" class="field-error" role="alert" aria-live="polite">
            </div>
        </div>
        
        <!-- Subject Selection -->
        <div class="field-group">
            <label for="inquiry-subject" class="field-label">
                Subject
                <span aria-label="required" class="required-indicator">*</span>
            </label>
            <select 
                id="inquiry-subject" 
                name="subject"
                class="field-select"
                required
                aria-describedby="subject-help subject-error"
                aria-invalid="false">
                
                <option value="">Please select a subject</option>
                <option value="general">General Inquiry</option>
                <option value="support">Technical Support</option>
                <option value="sales">Sales Question</option>
                <option value="partnership">Partnership Opportunity</option>
                <option value="feedback">Product Feedback</option>
            </select>
            
            <div id="subject-help" class="field-help">
                Choose the topic that best describes your inquiry
            </div>
            <div id="subject-error" class="field-error" role="alert" aria-live="polite">
            </div>
        </div>
        
        <!-- Message Textarea with Character Counter -->
        <div class="field-group">
            <label for="message-content" class="field-label">
                Message
                <span aria-label="required" class="required-indicator">*</span>
            </label>
            <textarea 
                id="message-content" 
                name="message"
                class="field-textarea"
                required
                aria-describedby="message-help message-counter message-error"
                aria-invalid="false"
                minlength="10"
                maxlength="1000"
                rows="6"
                placeholder="Please provide details about your inquiry..."></textarea>
            
            <div id="message-help" class="field-help">
                Minimum 10 characters, maximum 1,000 characters
            </div>
            <div id="message-counter" class="character-counter" aria-live="polite">
                <span id="current-count">0</span> / 1,000 characters
            </div>
            <div id="message-error" class="field-error" role="alert" aria-live="polite">
            </div>
        </div>
        
        <!-- Preference Checkboxes -->
        <fieldset class="checkbox-group">
            <legend class="checkbox-legend">Communication Preferences (Optional)</legend>
            
            <div class="checkbox-item">
                <input 
                    type="checkbox" 
                    id="newsletter-signup" 
                    name="preferences"
                    value="newsletter"
                    class="checkbox-input">
                <label for="newsletter-signup" class="checkbox-label">
                    Subscribe to our monthly newsletter
                </label>
            </div>
            
            <div class="checkbox-item">
                <input 
                    type="checkbox" 
                    id="product-updates" 
                    name="preferences"
                    value="updates"
                    class="checkbox-input">
                <label for="product-updates" class="checkbox-label">
                    Receive product updates and announcements
                </label>
            </div>
            
            <div class="checkbox-item">
                <input 
                    type="checkbox" 
                    id="marketing-consent" 
                    name="preferences"
                    value="marketing"
                    class="checkbox-input">
                <label for="marketing-consent" class="checkbox-label">
                    Allow marketing communications
                </label>
            </div>
        </fieldset>
        
        <!-- Privacy Policy Agreement -->
        <div class="field-group">
            <div class="checkbox-item required-checkbox">
                <input 
                    type="checkbox" 
                    id="privacy-agreement" 
                    name="privacyConsent"
                    required
                    aria-describedby="privacy-error"
                    class="checkbox-input">
                <label for="privacy-agreement" class="checkbox-label">
                    I agree to the 
                    <a href="/privacy-policy" target="_blank" rel="noopener">
                        Privacy Policy
                        <span class="sr-only">(opens in new window)</span>
                    </a>
                    <span aria-label="required" class="required-indicator">*</span>
                </label>
            </div>
            <div id="privacy-error" class="field-error" role="alert" aria-live="polite">
            </div>
        </div>
        
        <!-- Form Actions -->
        <div class="form-actions">
            <button type="submit" class="btn-primary" id="submit-button">
                <span class="btn-text">Send Message</span>
                <span class="btn-loading" aria-hidden="true">
                    <span class="loading-spinner"></span>
                    Sending...
                </span>
            </button>
            
            <button type="reset" class="btn-secondary">
                Clear Form
            </button>
        </div>
        
        <!-- Form Status Messages -->
        <div class="form-status" role="status" aria-live="polite" aria-atomic="true">
            <div id="success-message" class="success-message" role="alert">
                <strong>Thank you!</strong> Your message has been sent successfully. 
                We'll respond within 24 hours.
            </div>
            
            <div id="error-message" class="error-message" role="alert">
                <strong>Error:</strong> There was a problem sending your message. 
                Please check the form for errors and try again.
            </div>
        </div>
    </fieldset>
</form>

<!-- Form Enhancement Script -->
<script>
document.addEventListener('DOMContentLoaded', function() {
    const form = document.querySelector('.contact-form');
    const messageTextarea = document.getElementById('message-content');
    const charCounter = document.getElementById('current-count');
    const submitButton = document.getElementById('submit-button');
    
    // Real-time character counter
    messageTextarea.addEventListener('input', function() {
        const currentLength = this.value.length;
        charCounter.textContent = currentLength;
        
        // Update accessibility announcement
        if (currentLength > 950) {
            charCounter.parentElement.setAttribute('aria-live', 'assertive');
        } else {
            charCounter.parentElement.setAttribute('aria-live', 'polite');
        }
    });
    
    // Form validation and submission
    form.addEventListener('submit', async function(e) {
        e.preventDefault();
        
        // Clear previous errors
        clearFormErrors();
        
        // Validate form
        if (!validateForm()) {
            announceErrors();
            return;
        }
        
        // Submit form with loading state
        setSubmitLoading(true);
        
        try {
            const response = await submitForm(new FormData(form));
            if (response.ok) {
                showSuccessMessage();
                form.reset();
            } else {
                showErrorMessage('Server error. Please try again.');
            }
        } catch (error) {
            showErrorMessage('Network error. Please check your connection.');
        } finally {
            setSubmitLoading(false);
        }
    });
    
    function validateForm() {
        let isValid = true;
        
        // Validate required fields
        const requiredFields = form.querySelectorAll('[required]');
        requiredFields.forEach(field => {
            if (!field.value.trim()) {
                showFieldError(field, 'This field is required.');
                isValid = false;
            }
        });
        
        // Email validation
        const email = form.querySelector('#email-address');
        if (email.value && !isValidEmail(email.value)) {
            showFieldError(email, 'Please enter a valid email address.');
            isValid = false;
        }
        
        return isValid;
    }
    
    function showFieldError(field, message) {
        const errorElement = document.getElementById(field.getAttribute('aria-describedby').split(' ').find(id => id.includes('error')));
        errorElement.textContent = message;
        field.setAttribute('aria-invalid', 'true');
        field.classList.add('field-error-state');
    }
    
    function clearFormErrors() {
        const errorElements = form.querySelectorAll('.field-error');
        const fields = form.querySelectorAll('.field-input, .field-select, .field-textarea');
        
        errorElements.forEach(el => el.textContent = '');
        fields.forEach(field => {
            field.setAttribute('aria-invalid', 'false');
            field.classList.remove('field-error-state');
        });
    }
});
</script>
```

## Accessible Modal Dialog

**Code**:

```html
<!-- Modal Trigger -->
<button 
    type="button" 
    class="modal-trigger"
    data-modal="example-modal"
    aria-haspopup="dialog">
    Open Modal Dialog
</button>

<!-- Modal Container -->
<div 
    id="example-modal" 
    class="modal-overlay" 
    role="dialog" 
    aria-modal="true"
    aria-labelledby="modal-title"
    aria-describedby="modal-description"
    aria-hidden="true"
    tabindex="-1">
    
    <div class="modal-container">
        <div class="modal-content">
            <!-- Modal Header -->
            <header class="modal-header">
                <h2 id="modal-title" class="modal-title">
                    Confirmation Required
                </h2>
                <button 
                    type="button" 
                    class="modal-close"
                    aria-label="Close dialog">
                    <span aria-hidden="true">&times;</span>
                </button>
            </header>
            
            <!-- Modal Body -->
            <div class="modal-body">
                <p id="modal-description">
                    Are you sure you want to delete this item? 
                    This action cannot be undone.
                </p>
                
                <!-- Optional form or content -->
                <div class="modal-form">
                    <label for="confirmation-input" class="sr-only">
                        Type "DELETE" to confirm
                    </label>
                    <input 
                        type="text" 
                        id="confirmation-input"
                        placeholder='Type "DELETE" to confirm'
                        class="confirmation-input">
                </div>
            </div>
            
            <!-- Modal Footer -->
            <footer class="modal-footer">
                <button type="button" class="btn-secondary modal-cancel">
                    Cancel
                </button>
                <button type="button" class="btn-danger modal-confirm" disabled>
                    Delete Item
                </button>
            </footer>
        </div>
    </div>
</div>

<!-- Focus Trap and Modal Management Script -->
<script>
class AccessibleModal {
    constructor(modalId) {
        this.modal = document.getElementById(modalId);
        this.trigger = document.querySelector(`[data-modal="${modalId}"]`);
        this.closeButtons = this.modal.querySelectorAll('.modal-close, .modal-cancel');
        this.confirmButton = this.modal.querySelector('.modal-confirm');
        this.confirmInput = this.modal.querySelector('.confirmation-input');
        
        this.focusableElements = this.modal.querySelectorAll(
            'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
        );
        
        this.firstFocusableElement = this.focusableElements[0];
        this.lastFocusableElement = this.focusableElements[this.focusableElements.length - 1];
        
        this.init();
    }
    
    init() {
        // Trigger button
        this.trigger?.addEventListener('click', () => this.open());
        
        // Close buttons
        this.closeButtons.forEach(button => {
            button.addEventListener('click', () => this.close());
        });
        
        // Confirmation input
        this.confirmInput?.addEventListener('input', (e) => {
            const isValid = e.target.value.toUpperCase() === 'DELETE';
            this.confirmButton.disabled = !isValid;
        });
        
        // Keyboard navigation
        this.modal.addEventListener('keydown', (e) => this.handleKeyDown(e));
        
        // Backdrop close
        this.modal.addEventListener('click', (e) => {
            if (e.target === this.modal) this.close();
        });
    }
    
    open() {
        // Store currently focused element
        this.previousFocusedElement = document.activeElement;
        
        // Show modal
        this.modal.setAttribute('aria-hidden', 'false');
        this.modal.classList.add('modal-active');
        document.body.classList.add('modal-open');
        
        // Focus first element
        this.firstFocusableElement?.focus();
        
        // Announce to screen readers
        this.announce('Dialog opened');
    }
    
    close() {
        // Hide modal
        this.modal.setAttribute('aria-hidden', 'true');
        this.modal.classList.remove('modal-active');
        document.body.classList.remove('modal-open');
        
        // Reset form
        if (this.confirmInput) {
            this.confirmInput.value = '';
            this.confirmButton.disabled = true;
        }
        
        // Restore focus
        this.previousFocusedElement?.focus();
        
        // Announce to screen readers
        this.announce('Dialog closed');
    }
    
    handleKeyDown(event) {
        if (event.key === 'Escape') {
            this.close();
            return;
        }
        
        if (event.key === 'Tab') {
            // Trap focus within modal
            if (event.shiftKey) {
                if (document.activeElement === this.firstFocusableElement) {
                    event.preventDefault();
                    this.lastFocusableElement?.focus();
                }
            } else {
                if (document.activeElement === this.lastFocusableElement) {
                    event.preventDefault();
                    this.firstFocusableElement?.focus();
                }
            }
        }
    }
    
    announce(message) {
        const announcer = document.getElementById('announcements') || 
                         document.querySelector('[aria-live="assertive"]');
        if (announcer) {
            announcer.textContent = message;
            setTimeout(() => announcer.textContent = '', 1000);
        }
    }
}

// Initialize modal
document.addEventListener('DOMContentLoaded', () => {
    new AccessibleModal('example-modal');
});
</script>
```

**Usage**:
                            **Usage**:
                        </div>
                    </cite>
                </blockquote>
                <!-- More testimonials... -->
            </div>
        </div>
    </section>
    
    <!-- CTA Section -->
    <section class="cta">
        <div class="container">
            <h2>Ready to Get Started?</h2>
            <p>Join thousands of satisfied customers today.</p>
            <a href="#" class="btn btn-primary btn-large">Start Your Free Trial</a>
        </div>
    </section>
    
    <!-- Footer -->
    <footer class="footer">
        <div class="container">
            <div class="footer-content">
                <div class="footer-brand">
                    <img src="images/logo.svg" alt="Company Logo">
                    <p>Building the future of business solutions.</p>
                </div>
                <div class="footer-links">
                    <div class="link-group">
                        <h4>Product</h4>
                        <ul>
                            <li><a href="#">Features</a></li>
                            <li><a href="#">Pricing</a></li>
                            <li><a href="#">Documentation</a></li>
                        </ul>
                    </div>
                    <div class="link-group">
                        <h4>Support</h4>
                        <ul>
                            <li><a href="#">Help Center</a></li>
                            <li><a href="#">Contact Us</a></li>
                            <li><a href="#">Status</a></li>
                        </ul>
                    </div>
                </div>
            </div>
            <div class="footer-bottom">
                <p>&copy; 2024 Your Company. All rights reserved.</p>
                <div class="footer-legal">
                    <a href="#">Privacy Policy</a>
                    <a href="#">Terms of Service</a>
                </div>
            </div>
        </div>
    </footer>
    
    <script src="scripts/landing.js"></script>
</body>
</html>
```

## Form Templates

**Code**:

```html
<!-- Contact Form -->
<form class="contact-form" method="POST" action="/submit-contact">
    <div class="form-group">
        <label for="name">Full Name *</label>
        <input 
            type="text" 
            id="name" 
            name="name" 
            required 
            aria-describedby="name-error"
            autocomplete="name"
        >
        <div id="name-error" class="error-message" aria-live="polite"></div>
    </div>
    
    <div class="form-group">
        <label for="email">Email Address *</label>
        <input 
            type="email" 
            id="email" 
            name="email" 
            required 
            aria-describedby="email-error"
            autocomplete="email"
        >
        <div id="email-error" class="error-message" aria-live="polite"></div>
    </div>
    
    <div class="form-group">
        <label for="phone">Phone Number</label>
        <input 
            type="tel" 
            id="phone" 
            name="phone" 
            aria-describedby="phone-help"
            autocomplete="tel"
        >
        <div id="phone-help" class="help-text">Format: (123) 456-7890</div>
    </div>
    
    <div class="form-group">
        <label for="subject">Subject *</label>
        <select id="subject" name="subject" required>
            <option value="">Select a subject</option>
            <option value="general">General Inquiry</option>
            <option value="support">Technical Support</option>
            <option value="sales">Sales</option>
            <option value="partnership">Partnership</option>
        </select>
    </div>
    
    <div class="form-group">
        <label for="message">Message *</label>
        <textarea 
            id="message" 
            name="message" 
            rows="5" 
            required 
            placeholder="Tell us how we can help you..."
            aria-describedby="message-counter"
        ></textarea>
        <div id="message-counter" class="character-counter">0 / 500 characters</div>
    </div>
    
    <div class="form-group checkbox-group">
        <input type="checkbox" id="newsletter" name="newsletter" value="yes">
        <label for="newsletter">Subscribe to our newsletter for updates</label>
    </div>
    
    <div class="form-group checkbox-group">
        <input type="checkbox" id="privacy" name="privacy" required>
        <label for="privacy">
            I agree to the <a href="/privacy" target="_blank">Privacy Policy</a> *
        </label>
    </div>
    
    <div class="form-actions">
        <button type="submit" class="btn btn-primary">Send Message</button>
        <button type="reset" class="btn btn-secondary">Clear Form</button>
    </div>
</form>

<!-- Registration Form -->
<form class="registration-form" method="POST" action="/register">
    <fieldset>
        <legend>Account Information</legend>
        
        <div class="form-row">
            <div class="form-group">
                <label for="first-name">First Name *</label>
                <input 
                    type="text" 
                    id="first-name" 
                    name="firstName" 
                    required 
                    autocomplete="given-name"
                >
            </div>
            <div class="form-group">
                <label for="last-name">Last Name *</label>
                <input 
                    type="text" 
                    id="last-name" 
                    name="lastName" 
                    required 
                    autocomplete="family-name"
                >
            </div>
        </div>
        
        <div class="form-group">
            <label for="username">Username *</label>
            <input 
                type="text" 
                id="username" 
                name="username" 
                required 
                minlength="3"
                maxlength="20"
                pattern="[a-zA-Z0-9_]+"
                aria-describedby="username-help"
                autocomplete="username"
            >
            <div id="username-help" class="help-text">
                3-20 characters, letters, numbers, and underscores only
            </div>
        </div>
        
        <div class="form-group">
            <label for="register-email">Email Address *</label>
            <input 
                type="email" 
                id="register-email" 
                name="email" 
                required 
                autocomplete="email"
            >
        </div>
        
        <div class="form-group">
            <label for="password">Password *</label>
            <input 
                type="password" 
                id="password" 
                name="password" 
                required 
                minlength="8"
                aria-describedby="password-requirements"
                autocomplete="new-password"
            >
            <div id="password-requirements" class="help-text">
                Minimum 8 characters with uppercase, lowercase, number, and symbol
            </div>
            <div class="password-strength">
                <div class="strength-meter">
                    <div class="strength-fill"></div>
                </div>
                <span class="strength-text">Password strength: Weak</span>
            </div>
        </div>
        
        <div class="form-group">
            <label for="confirm-password">Confirm Password *</label>
            <input 
                type="password" 
                id="confirm-password" 
                name="confirmPassword" 
                required 
                autocomplete="new-password"
            >
        </div>
    </fieldset>
    
    <fieldset>
        <legend>Personal Information</legend>
        
        <div class="form-group">
            <label for="birth-date">Date of Birth</label>
            <input 
                type="date" 
                id="birth-date" 
                name="birthDate" 
                autocomplete="bday"
            >
        </div>
        
        <div class="form-group">
            <fieldset class="radio-group">
                <legend>Gender</legend>
                <div class="radio-options">
                    <label>
                        <input type="radio" name="gender" value="male">
                        Male
                    </label>
                    <label>
                        <input type="radio" name="gender" value="female">
                        Female
                    </label>
                    <label>
                        <input type="radio" name="gender" value="other">
                        Other
                    </label>
                    <label>
                        <input type="radio" name="gender" value="prefer-not-to-say">
                        Prefer not to say
                    </label>
                </div>
            </fieldset>
        </div>
    </fieldset>
    
    <div class="form-actions">
        <button type="submit" class="btn btn-primary">Create Account</button>
    </div>
</form>
```

## Accessible Components

**Code**:

```html
<!-- Accessible Modal Dialog -->
<div class="modal-overlay" id="modal-overlay" aria-hidden="true">
    <div class="modal" role="dialog" aria-labelledby="modal-title" aria-describedby="modal-description">
        <div class="modal-header">
            <h2 id="modal-title">Modal Title</h2>
            <button type="button" class="modal-close" aria-label="Close modal">
                <svg aria-hidden="true" width="24" height="24" viewBox="0 0 24 24">
                    <path d="M6 18L18 6M6 6l12 12" stroke="currentColor" stroke-width="2"/>
                </svg>
            </button>
        </div>
        <div class="modal-body">
            <p id="modal-description">Modal content goes here.</p>
        </div>
        <div class="modal-footer">
            <button type="button" class="btn btn-secondary">Cancel</button>
            <button type="button" class="btn btn-primary">Confirm</button>
        </div>
    </div>
</div>

<!-- Accessible Tab Component -->
<div class="tabs">
    <div class="tab-list" role="tablist" aria-label="Content sections">
        <button 
            class="tab" 
            role="tab" 
            aria-selected="true" 
            aria-controls="tab-panel-1" 
            id="tab-1"
        >
            Tab 1
        </button>
        <button 
            class="tab" 
            role="tab" 
            aria-selected="false" 
            aria-controls="tab-panel-2" 
            id="tab-2"
            tabindex="-1"
        >
            Tab 2
        </button>
        <button 
            class="tab" 
            role="tab" 
            aria-selected="false" 
            aria-controls="tab-panel-3" 
            id="tab-3"
            tabindex="-1"
        >
            Tab 3
        </button>
    </div>
    
    <div class="tab-panels">
        <div 
            class="tab-panel" 
            role="tabpanel" 
            id="tab-panel-1" 
            aria-labelledby="tab-1"
        >
            Content for tab 1
        </div>
        <div 
            class="tab-panel" 
            role="tabpanel" 
            id="tab-panel-2" 
            aria-labelledby="tab-2"
            hidden
        >
            Content for tab 2
        </div>
        <div 
            class="tab-panel" 
            role="tabpanel" 
            id="tab-panel-3" 
            aria-labelledby="tab-3"
            hidden
        >
            Content for tab 3
        </div>
    </div>
</div>

<!-- Accessible Dropdown Menu -->
<div class="dropdown">
    <button 
        class="dropdown-toggle" 
        aria-expanded="false" 
        aria-haspopup="menu" 
        id="dropdown-button"
    >
        Menu
        <svg class="dropdown-arrow" width="12" height="12" viewBox="0 0 12 12">
            <path d="M2 4l4 4 4-4" stroke="currentColor" fill="none"/>
        </svg>
    </button>
    
    <ul 
        class="dropdown-menu" 
        role="menu" 
        aria-labelledby="dropdown-button"
        hidden
    >
        <li role="none">
            <a href="#" role="menuitem">Menu Item 1</a>
        </li>
        <li role="none">
            <a href="#" role="menuitem">Menu Item 2</a>
        </li>
        <li role="none">
            <button type="button" role="menuitem">Menu Action</button>
        </li>
        <li role="separator" class="dropdown-divider"></li>
        <li role="none">
            <a href="#" role="menuitem">Menu Item 3</a>
        </li>
    </ul>
</div>
```

**Usage**:

```html
<!-- Progressive Web App Implementation -->
<!DOCTYPE html>
<html lang="en" data-theme="light">
<head>
    <!-- Critical meta tags and performance optimization -->
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0, viewport-fit=cover">
    <link rel="manifest" href="/manifest.json">
    
    <!-- Security headers and content policy -->
    <meta http-equiv="Content-Security-Policy" content="default-src 'self'; script-src 'self' 'unsafe-inline';">
    
    <!-- Critical CSS inline for performance -->
    <style>/* Critical above-the-fold styles */</style>
    
    <!-- Non-critical CSS with performance loading -->
    <link rel="stylesheet" href="/assets/css/main.css" media="print" onload="this.media='all'">
    <noscript><link rel="stylesheet" href="/assets/css/main.css"></noscript>
</head>
<body>
    <!-- Skip navigation for accessibility -->
    <a href="#main-content" class="skip-link">Skip to main content</a>
    
    <!-- Semantic structure with ARIA landmarks -->
    <header role="banner">
        <nav role="navigation" aria-label="Main navigation">
            <!-- Accessible navigation with proper ARIA -->
        </nav>
    </header>
    
    <main id="main-content" role="main">
        <!-- Main content with proper heading hierarchy -->
    </main>
    
    <footer role="contentinfo">
        <!-- Footer with company information and links -->
    </footer>
    
    <!-- Accessibility announcements -->
    <div id="announcements" aria-live="assertive" aria-atomic="true" class="sr-only"></div>
    
    <!-- Modern JavaScript with progressive enhancement -->
    <script type="module" src="/assets/js/main.js"></script>
    <script nomodule src="/assets/js/main.legacy.js"></script>
</body>
</html>

<!-- Accessible Form Implementation -->
<form method="post" action="/contact" novalidate aria-labelledby="form-title">
    <fieldset>
        <legend id="form-title">Contact Information</legend>
        
        <!-- Comprehensive field with error handling -->
        <div class="field-group">
            <label for="full-name">
                Full Name
                <span aria-label="required" class="required-indicator">*</span>
            </label>
            <input 
                type="text" 
                id="full-name" 
                name="fullName"
                required
                aria-describedby="name-help name-error"
                aria-invalid="false"
                autocomplete="name">
            <div id="name-help" class="field-help">Enter your first and last name</div>
            <div id="name-error" class="field-error" role="alert" aria-live="polite"></div>
        </div>
        
        <!-- Form submission with loading states -->
        <button type="submit" class="btn-primary">
            <span class="btn-text">Send Message</span>
            <span class="btn-loading" aria-hidden="true">Sending...</span>
        </button>
    </fieldset>
</form>

<!-- Modal Dialog with Focus Management -->
<div id="example-modal" role="dialog" aria-modal="true" aria-hidden="true">
    <div class="modal-content">
        <header class="modal-header">
            <h2 id="modal-title">Dialog Title</h2>
            <button type="button" class="modal-close" aria-label="Close dialog">×</button>
        </header>
        <div class="modal-body" aria-describedby="modal-description">
            <!-- Modal content with proper ARIA relationships -->
        </div>
        <footer class="modal-footer">
            <button type="button" class="btn-secondary">Cancel</button>
            <button type="button" class="btn-primary">Confirm</button>
        </footer>
    </div>
</div>
```

**Notes**:

- **🚀 Performance First**: Critical CSS inline, non-critical CSS loaded asynchronously, resource hints for optimization
- **♿ WCAG 2.1 AA Compliance**: Complete keyboard navigation, screen reader optimization, proper color contrast
- **🛡️ Security Hardened**: Content Security Policy headers, input validation, XSS prevention, secure communication
- **📱 Mobile Optimized**: Mobile-first responsive design, touch-friendly interfaces, progressive enhancement
- **🔍 SEO Enhanced**: Semantic HTML structure, structured data markup, proper meta tags and Open Graph
- **🌐 Progressive Web App**: Service workers for offline functionality, web app manifest, modern web APIs
- **⚡ Core Web Vitals**: Optimized loading performance, efficient resource management, smooth interactions
- **🎯 Accessibility**: Skip navigation links, ARIA landmarks and labels, focus management, screen reader support
- **🔧 Developer Experience**: Modern JavaScript modules, comprehensive error handling, development tooling integration
- **✅ Cross-Browser**: Progressive enhancement, feature detection, fallbacks for legacy browsers
- **📊 Analytics Ready**: Proper event tracking structure, performance monitoring hooks, user experience metrics
- **🔐 Privacy Compliant**: GDPR considerations, cookie consent patterns, data protection measures

## Implementation Guidelines

### Critical Performance Checklist
- ✅ Inline critical above-the-fold CSS (< 14KB)
- ✅ Preload essential fonts and resources
- ✅ Optimize images with modern formats (WebP/AVIF)
- ✅ Use resource hints (preconnect, prefetch, preload)
- ✅ Implement service workers for caching strategies

### Accessibility Validation
- ✅ Test with screen readers (NVDA, JAWS, VoiceOver)
- ✅ Verify complete keyboard navigation
- ✅ Check color contrast ratios (WCAG AA minimum)
- ✅ Validate HTML structure and semantics
- ✅ Test with accessibility auditing tools

### Security Implementation
- ✅ Configure Content Security Policy headers
- ✅ Implement input validation and sanitization
- ✅ Use HTTPS with proper certificate configuration
- ✅ Secure cookie settings and session management
- ✅ Regular dependency security audits

## Related Snippets

- [CSS Layouts](css-layouts.md) - Modern CSS Grid and Flexbox layout systems
- [Accessibility](accessibility.md) - Comprehensive WCAG 2.1 compliance patterns
- [Performance Optimization](../utilities/readme.md) - Web performance best practices
