---
description: HTML/Web markup standards and accessibility best practices
applyTo: '**/*.{html,htm,xhtml}'
---

# HTML / Web Markup Instructions

## Scope
Applies to `.html`, `.htm`, `.xhtml` files and HTML markup development.

## Document Structure
- Use HTML5 semantic elements (`<header>`, `<nav>`, `<main>`, `<aside>`, `<footer>`).
- Include proper DOCTYPE declaration: `<!DOCTYPE html>`.
- Use valid, well-formed markup with proper nesting.
- Include `<meta charset="UTF-8">` for character encoding.
- Use `<meta name="viewport">` for responsive design.

## Semantic Markup
- Choose elements based on meaning, not appearance.
- Use headings (`h1`-`h6`) in logical hierarchy.
- Use `<article>`, `<section>`, and `<div>` appropriately.
- Use `<button>` for interactive elements, not `<div>`.
- Use `<a>` tags only for navigation, not generic interactions.

## Accessibility Standards
- Include `alt` attributes for all images.
- Use `aria-label` and `aria-describedby` for screen readers.
- Ensure proper color contrast ratios.
- Make all interactive elements keyboard accessible.
- Use `role` attributes when semantic elements aren't sufficient.
- Include skip links for keyboard navigation.

## Form Best Practices
- Associate labels with form controls using `for` attribute.
- Use appropriate input types (`email`, `tel`, `url`, `date`).
- Include `required` and validation attributes.
- Provide clear error messages and instructions.
- Group related form elements with `<fieldset>` and `<legend>`.

## Naming Conventions
- Use lowercase for all element names and attributes.
- Use hyphens in class and ID names: `main-navigation`.
- Choose descriptive, semantic class names.
- Avoid styling-based names: use `primary-button` not `red-button`.
- Keep ID values unique throughout the document.

## Performance Optimization
- Minimize HTML file size and DOM complexity.
- Use external stylesheets and scripts when possible.
- Optimize images and use appropriate formats.
- Implement lazy loading for images and content.
- Use resource hints (`preload`, `prefetch`, `dns-prefetch`).

## SEO Best Practices
- Use descriptive, unique title tags for each page.
- Include meta descriptions with relevant keywords.
- Use heading tags to structure content hierarchy.
- Implement structured data markup when appropriate.
- Create clean, descriptive URL structures.

## Cross-Browser Compatibility
- Test in multiple browsers and devices.
- Use progressive enhancement principles.
- Validate markup using W3C validator.
- Handle older browser support gracefully.
- Use feature detection instead of browser detection.

## Security Considerations
- Sanitize user input and output.
- Use HTTPS for all external resources.
- Implement proper Content Security Policy (CSP).
- Validate and escape dynamic content.
- Use secure cookie settings when applicable.

## Code Organization
- Indent nested elements consistently (2 or 4 spaces).
- Use meaningful comments for complex sections.
- Group related elements logically.
- Keep line length reasonable for readability.
- Separate content, presentation, and behavior.

## Modern HTML Features
- Use custom elements and Web Components appropriately.
- Implement proper `<template>` usage.
- Use `<picture>` element for responsive images.
- Leverage `<details>` and `<summary>` for collapsible content.
- Use `data-*` attributes for custom data storage.

## Validation and Testing
- Validate HTML markup regularly.
- Test with screen readers and accessibility tools.
- Verify responsive behavior across devices.
- Check performance with Lighthouse audits.
- Test form functionality thoroughly.

## Documentation
- Comment complex markup structures.
- Document custom attributes and their purposes.
- Maintain style guides for consistent markup.
- Include accessibility requirements in documentation.
- Document browser support requirements.