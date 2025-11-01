# HTML Templates

**Description**: Reusable HTML templates and boilerplate code for web development
**Language/Technology**: HTML5 / Web Development

## Basic HTML5 Template

**Code**:

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <meta name="description" content="Page description for SEO">
    <meta name="keywords" content="relevant, keywords, for, seo">
    <meta name="author" content="Author Name">
    
    <!-- Open Graph meta tags for social media -->
    <meta property="og:title" content="Page Title">
    <meta property="og:description" content="Page description">
    <meta property="og:image" content="/images/og-image.jpg">
    <meta property="og:url" content="https://example.com/page">
    <meta property="og:type" content="website">
    
    <!-- Twitter Card meta tags -->
    <meta name="twitter:card" content="summary_large_image">
    <meta name="twitter:title" content="Page Title">
    <meta name="twitter:description" content="Page description">
    <meta name="twitter:image" content="/images/twitter-image.jpg">
    
    <title>Page Title | Site Name</title>
    
    <!-- Favicon -->
    <link rel="icon" type="image/x-icon" href="/favicon.ico">
    <link rel="apple-touch-icon" sizes="180x180" href="/apple-touch-icon.png">
    
    <!-- Stylesheets -->
    <link rel="stylesheet" href="styles/main.css">
    
    <!-- Preload critical resources -->
    <link rel="preload" href="fonts/main.woff2" as="font" type="font/woff2" crossorigin>
</head>
<body>
    <header>
        <nav>
            <!-- Navigation content -->
        </nav>
    </header>
    
    <main>
        <section>
            <!-- Main content -->
        </section>
    </main>
    
    <footer>
        <!-- Footer content -->
    </footer>
    
    <!-- Scripts before closing body tag -->
    <script src="scripts/main.js"></script>
</body>
</html>
```

## Landing Page Template

**Code**:

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <meta name="description" content="Professional landing page for your business">
    <title>Landing Page | Your Business</title>
    <link rel="stylesheet" href="styles/landing.css">
</head>
<body>
    <!-- Hero Section -->
    <section class="hero">
        <div class="container">
            <header class="hero-header">
                <nav class="navbar">
                    <div class="nav-brand">
                        <img src="images/logo.svg" alt="Company Logo" class="logo">
                    </div>
                    <ul class="nav-menu">
                        <li><a href="#features">Features</a></li>
                        <li><a href="#pricing">Pricing</a></li>
                        <li><a href="#contact">Contact</a></li>
                        <li><a href="#" class="cta-button">Sign Up</a></li>
                    </ul>
                    <button class="hamburger" aria-label="Toggle menu">
                        <span></span>
                        <span></span>
                        <span></span>
                    </button>
                </nav>
            </header>
            
            <div class="hero-content">
                <h1 class="hero-title">Transform Your Business Today</h1>
                <p class="hero-subtitle">
                    Powerful solutions that help you grow faster and reach more customers
                </p>
                <div class="hero-actions">
                    <a href="#" class="btn btn-primary">Get Started Free</a>
                    <a href="#" class="btn btn-secondary">Watch Demo</a>
                </div>
            </div>
        </div>
    </section>
    
    <!-- Features Section -->
    <section id="features" class="features">
        <div class="container">
            <h2 class="section-title">Why Choose Us</h2>
            <div class="features-grid">
                <div class="feature-card">
                    <div class="feature-icon">
                        <svg><!-- Feature icon --></svg>
                    </div>
                    <h3>Fast & Reliable</h3>
                    <p>Lightning-fast performance with 99.9% uptime guarantee.</p>
                </div>
                <div class="feature-card">
                    <div class="feature-icon">
                        <svg><!-- Feature icon --></svg>
                    </div>
                    <h3>Secure</h3>
                    <p>Enterprise-grade security to protect your data.</p>
                </div>
                <div class="feature-card">
                    <div class="feature-icon">
                        <svg><!-- Feature icon --></svg>
                    </div>
                    <h3>Easy to Use</h3>
                    <p>Intuitive interface that anyone can master.</p>
                </div>
            </div>
        </div>
    </section>
    
    <!-- Testimonials Section -->
    <section class="testimonials">
        <div class="container">
            <h2 class="section-title">What Our Customers Say</h2>
            <div class="testimonials-grid">
                <blockquote class="testimonial">
                    <p>"This product transformed how we do business. Highly recommended!"</p>
                    <cite>
                        <img src="images/avatar1.jpg" alt="Customer photo">
                        <div>
                            <strong>Jane Doe</strong>
                            <span>CEO, Tech Corp</span>
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
<!-- Basic usage -->
<!-- Include the HTML5 template for new pages -->
<!-- Customize meta tags for each page's content -->
<!-- Add appropriate semantic elements -->

<!-- Forms -->
<!-- Always include proper labels and validation -->
<!-- Use autocomplete attributes for better UX -->
<!-- Implement client-side validation with JavaScript -->

<!-- Accessibility -->
<!-- Test with screen readers -->
<!-- Ensure keyboard navigation works -->
<!-- Check color contrast ratios -->
<!-- Use semantic HTML elements -->
```

**Notes**:

- **SEO**: Include proper meta tags and structured data
- **Performance**: Optimize images and minify resources
- **Accessibility**: Follow WCAG guidelines for inclusive design
- **Mobile**: Test on various device sizes and orientations
- **Security**: Validate and sanitize all form inputs
- **Standards**: Use semantic HTML5 elements appropriately
- **Progressive Enhancement**: Ensure basic functionality without JavaScript
- **Testing**: Validate HTML and test across browsers

## Related Snippets

- [CSS Layouts](css-layouts.md) - Styling for HTML templates
- [Responsive Design](responsive-design.md) - Mobile-first approaches
- [Accessibility](accessibility.md) - WCAG compliance patterns
