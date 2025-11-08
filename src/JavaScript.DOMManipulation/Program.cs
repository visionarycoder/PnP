namespace JavaScript.DOMManipulation;

/// <summary>
/// Demonstrates JavaScript DOM manipulation patterns and best practices
/// </summary>
public static class Program
{
    public static void Main()
    {
        Console.WriteLine("JavaScript DOM Manipulation Examples");
        Console.WriteLine("===================================");
        
        ElementSelectionPatterns();
        ElementCreationPatterns();
        EventHandlingPatterns();
        DynamicContentPatterns();
        PerformanceOptimizations();
        ModernDOMPatterns();
    }

    /// <summary>
    /// Element selection and traversal patterns
    /// </summary>
    private static void ElementSelectionPatterns()
    {
        Console.WriteLine("\n1. Element Selection Patterns:");
        
        Console.WriteLine("// Modern selectors (preferred)");
        Console.WriteLine("const element = document.querySelector('.my-class');");
        Console.WriteLine("const elements = document.querySelectorAll('div[data-active=\"true\"]');");
        Console.WriteLine("const form = document.querySelector('form#user-form');");
        
        Console.WriteLine("\n// Traditional selectors (less flexible)");
        Console.WriteLine("const elementById = document.getElementById('my-id');");
        Console.WriteLine("const elementsByClass = document.getElementsByClassName('my-class');");
        Console.WriteLine("const elementsByTag = document.getElementsByTagName('div');");
        
        Console.WriteLine("\n// Traversal patterns");
        Console.WriteLine("const parent = element.parentElement;");
        Console.WriteLine("const nextSibling = element.nextElementSibling;");
        Console.WriteLine("const children = element.children;");
        Console.WriteLine("const firstChild = element.firstElementChild;");
        
        Console.WriteLine("\n// Advanced selection with context");
        Console.WriteLine("const container = document.querySelector('.container');");
        Console.WriteLine("const nestedElement = container.querySelector('.nested-item');");
        Console.WriteLine("const allNestedItems = container.querySelectorAll('.item');");
    }

    /// <summary>
    /// Dynamic element creation patterns
    /// </summary>
    private static void ElementCreationPatterns()
    {
        Console.WriteLine("\n2. Element Creation Patterns:");
        
        Console.WriteLine("// Basic element creation");
        Console.WriteLine("const div = document.createElement('div');");
        Console.WriteLine("div.className = 'my-class';");
        Console.WriteLine("div.id = 'my-id';");
        Console.WriteLine("div.textContent = 'Hello World';");
        
        Console.WriteLine("\n// Setting attributes");
        Console.WriteLine("div.setAttribute('data-value', '123');");
        Console.WriteLine("div.setAttribute('aria-label', 'Description');");
        Console.WriteLine("div.dataset.userId = '456'; // Sets data-user-id");
        
        Console.WriteLine("\n// Creating complex structures");
        Console.WriteLine("function createCard(title, description, imageUrl) {");
        Console.WriteLine("  const card = document.createElement('div');");
        Console.WriteLine("  card.className = 'card';");
        Console.WriteLine("  ");
        Console.WriteLine("  const img = document.createElement('img');");
        Console.WriteLine("  img.src = imageUrl;");
        Console.WriteLine("  img.alt = title;");
        Console.WriteLine("  ");
        Console.WriteLine("  const content = document.createElement('div');");
        Console.WriteLine("  content.className = 'card-content';");
        Console.WriteLine("  ");
        Console.WriteLine("  const titleEl = document.createElement('h3');");
        Console.WriteLine("  titleEl.textContent = title;");
        Console.WriteLine("  ");
        Console.WriteLine("  const descEl = document.createElement('p');");
        Console.WriteLine("  descEl.textContent = description;");
        Console.WriteLine("  ");
        Console.WriteLine("  content.appendChild(titleEl);");
        Console.WriteLine("  content.appendChild(descEl);");
        Console.WriteLine("  card.appendChild(img);");
        Console.WriteLine("  card.appendChild(content);");
        Console.WriteLine("  ");
        Console.WriteLine("  return card;");
        Console.WriteLine("}");
        
        Console.WriteLine("\n// Using innerHTML (be careful with XSS)");
        Console.WriteLine("const safeHtml = `");
        Console.WriteLine("  <div class=\"notification ${type}\">");
        Console.WriteLine("    <span class=\"icon\">${icon}</span>");
        Console.WriteLine("    <span class=\"message\">${escapeHtml(message)}</span>");
        Console.WriteLine("  </div>");
        Console.WriteLine("`;");
        Console.WriteLine("container.innerHTML = safeHtml;");
    }

    /// <summary>
    /// Event handling patterns and best practices
    /// </summary>
    private static void EventHandlingPatterns()
    {
        Console.WriteLine("\n3. Event Handling Patterns:");
        
        Console.WriteLine("// Modern event listeners");
        Console.WriteLine("button.addEventListener('click', (event) => {");
        Console.WriteLine("  event.preventDefault();");
        Console.WriteLine("  console.log('Button clicked!');");
        Console.WriteLine("});");
        
        Console.WriteLine("\n// Event delegation (efficient for dynamic content)");
        Console.WriteLine("document.addEventListener('click', (event) => {");
        Console.WriteLine("  if (event.target.matches('.dynamic-button')) {");
        Console.WriteLine("    handleDynamicButtonClick(event);");
        Console.WriteLine("  }");
        Console.WriteLine("});");
        
        Console.WriteLine("\n// Custom event patterns");
        Console.WriteLine("// Dispatching custom events");
        Console.WriteLine("const customEvent = new CustomEvent('userAction', {");
        Console.WriteLine("  detail: { userId: 123, action: 'login' },");
        Console.WriteLine("  bubbles: true");
        Console.WriteLine("});");
        Console.WriteLine("element.dispatchEvent(customEvent);");
        
        Console.WriteLine("\n// Listening for custom events");
        Console.WriteLine("document.addEventListener('userAction', (event) => {");
        Console.WriteLine("  const { userId, action } = event.detail;");
        Console.WriteLine("  console.log(`User ${userId} performed: ${action}`);");
        Console.WriteLine("});");
        
        Console.WriteLine("\n// Removing event listeners (prevent memory leaks)");
        Console.WriteLine("function handleClick(event) {");
        Console.WriteLine("  console.log('Clicked');");
        Console.WriteLine("}");
        Console.WriteLine("element.addEventListener('click', handleClick);");
        Console.WriteLine("element.removeEventListener('click', handleClick);");
        
        Console.WriteLine("\n// Event listener options");
        Console.WriteLine("element.addEventListener('scroll', handleScroll, {");
        Console.WriteLine("  passive: true,  // Better performance");
        Console.WriteLine("  once: true,     // Remove after first trigger");
        Console.WriteLine("  capture: false  // Event phase");
        Console.WriteLine("});");
    }

    /// <summary>
    /// Dynamic content manipulation patterns
    /// </summary>
    private static void DynamicContentPatterns()
    {
        Console.WriteLine("\n4. Dynamic Content Patterns:");
        
        Console.WriteLine("// Adding/removing classes");
        Console.WriteLine("element.classList.add('active', 'highlighted');");
        Console.WriteLine("element.classList.remove('inactive');");
        Console.WriteLine("element.classList.toggle('visible');");
        Console.WriteLine("element.classList.contains('active'); // Returns boolean");
        
        Console.WriteLine("\n// Style manipulation");
        Console.WriteLine("element.style.display = 'none';");
        Console.WriteLine("element.style.backgroundColor = '#ff0000';");
        Console.WriteLine("element.style.setProperty('--custom-property', 'value');");
        
        Console.WriteLine("\n// Content updates");
        Console.WriteLine("element.textContent = 'Safe text content';");
        Console.WriteLine("element.innerHTML = sanitizedHtml; // Use with caution");
        Console.WriteLine("element.insertAdjacentHTML('beforeend', '<p>New paragraph</p>');");
        
        Console.WriteLine("\n// Form manipulation");
        Console.WriteLine("const formData = new FormData(form);");
        Console.WriteLine("const inputValue = form.elements.username.value;");
        Console.WriteLine("form.elements.email.disabled = true;");
        Console.WriteLine("form.reset(); // Clear all form fields");
        
        Console.WriteLine("\n// List manipulation");
        Console.WriteLine("function addListItem(list, text) {");
        Console.WriteLine("  const li = document.createElement('li');");
        Console.WriteLine("  li.textContent = text;");
        Console.WriteLine("  li.addEventListener('click', () => li.remove());");
        Console.WriteLine("  list.appendChild(li);");
        Console.WriteLine("}");
        
        Console.WriteLine("\n// Batch DOM updates (performance)");
        Console.WriteLine("const fragment = document.createDocumentFragment();");
        Console.WriteLine("items.forEach(item => {");
        Console.WriteLine("  const element = createListItem(item);");
        Console.WriteLine("  fragment.appendChild(element);");
        Console.WriteLine("});");
        Console.WriteLine("container.appendChild(fragment); // Single reflow");
    }

    /// <summary>
    /// Performance optimization techniques
    /// </summary>
    private static void PerformanceOptimizations()
    {
        Console.WriteLine("\n5. Performance Optimizations:");
        
        Console.WriteLine("// Debouncing expensive operations");
        Console.WriteLine("function debounce(func, delay) {");
        Console.WriteLine("  let timeoutId;");
        Console.WriteLine("  return (...args) => {");
        Console.WriteLine("    clearTimeout(timeoutId);");
        Console.WriteLine("    timeoutId = setTimeout(() => func.apply(this, args), delay);");
        Console.WriteLine("  };");
        Console.WriteLine("}");
        Console.WriteLine("const debouncedSearch = debounce(performSearch, 300);");
        Console.WriteLine("searchInput.addEventListener('input', debouncedSearch);");
        
        Console.WriteLine("\n// Throttling scroll events");
        Console.WriteLine("function throttle(func, limit) {");
        Console.WriteLine("  let inThrottle;");
        Console.WriteLine("  return (...args) => {");
        Console.WriteLine("    if (!inThrottle) {");
        Console.WriteLine("      func.apply(this, args);");
        Console.WriteLine("      inThrottle = true;");
        Console.WriteLine("      setTimeout(() => inThrottle = false, limit);");
        Console.WriteLine("    }");
        Console.WriteLine("  };");
        Console.WriteLine("}");
        Console.WriteLine("const throttledScroll = throttle(handleScroll, 100);");
        Console.WriteLine("window.addEventListener('scroll', throttledScroll);");
        
        Console.WriteLine("\n// Intersection Observer for lazy loading");
        Console.WriteLine("const imageObserver = new IntersectionObserver((entries) => {");
        Console.WriteLine("  entries.forEach(entry => {");
        Console.WriteLine("    if (entry.isIntersecting) {");
        Console.WriteLine("      const img = entry.target;");
        Console.WriteLine("      img.src = img.dataset.src;");
        Console.WriteLine("      img.classList.add('loaded');");
        Console.WriteLine("      imageObserver.unobserve(img);");
        Console.WriteLine("    }");
        Console.WriteLine("  });");
        Console.WriteLine("});");
        Console.WriteLine("document.querySelectorAll('img[data-src]')");
        Console.WriteLine("  .forEach(img => imageObserver.observe(img));");
        
        Console.WriteLine("\n// RequestAnimationFrame for smooth animations");
        Console.WriteLine("function animate(element, property, start, end, duration) {");
        Console.WriteLine("  const startTime = performance.now();");
        Console.WriteLine("  ");
        Console.WriteLine("  function step(currentTime) {");
        Console.WriteLine("    const elapsed = currentTime - startTime;");
        Console.WriteLine("    const progress = Math.min(elapsed / duration, 1);");
        Console.WriteLine("    ");
        Console.WriteLine("    const value = start + (end - start) * progress;");
        Console.WriteLine("    element.style[property] = value + 'px';");
        Console.WriteLine("    ");
        Console.WriteLine("    if (progress < 1) {");
        Console.WriteLine("      requestAnimationFrame(step);");
        Console.WriteLine("    }");
        Console.WriteLine("  }");
        Console.WriteLine("  ");
        Console.WriteLine("  requestAnimationFrame(step);");
        Console.WriteLine("}");
    }

    /// <summary>
    /// Modern DOM APIs and patterns
    /// </summary>
    private static void ModernDOMPatterns()
    {
        Console.WriteLine("\n6. Modern DOM APIs:");
        
        Console.WriteLine("// Resize Observer");
        Console.WriteLine("const resizeObserver = new ResizeObserver(entries => {");
        Console.WriteLine("  entries.forEach(entry => {");
        Console.WriteLine("    console.log('Element resized:', entry.contentRect);");
        Console.WriteLine("  });");
        Console.WriteLine("});");
        Console.WriteLine("resizeObserver.observe(element);");
        
        Console.WriteLine("\n// Mutation Observer");
        Console.WriteLine("const mutationObserver = new MutationObserver(mutations => {");
        Console.WriteLine("  mutations.forEach(mutation => {");
        Console.WriteLine("    if (mutation.type === 'childList') {");
        Console.WriteLine("      mutation.addedNodes.forEach(node => {");
        Console.WriteLine("        if (node.nodeType === 1) { // Element node");
        Console.WriteLine("          console.log('Element added:', node);");
        Console.WriteLine("        }");
        Console.WriteLine("      });");
        Console.WriteLine("    }");
        Console.WriteLine("  });");
        Console.WriteLine("});");
        Console.WriteLine("mutationObserver.observe(container, {");
        Console.WriteLine("  childList: true,");
        Console.WriteLine("  subtree: true");
        Console.WriteLine("});");
        
        Console.WriteLine("\n// Web Components integration");
        Console.WriteLine("class CustomButton extends HTMLElement {");
        Console.WriteLine("  connectedCallback() {");
        Console.WriteLine("    this.innerHTML = `");
        Console.WriteLine("      <button class=\"custom-btn\">");
        Console.WriteLine("        <slot></slot>");
        Console.WriteLine("      </button>");
        Console.WriteLine("    `;");
        Console.WriteLine("    ");
        Console.WriteLine("    this.querySelector('button').addEventListener('click', () => {");
        Console.WriteLine("      this.dispatchEvent(new CustomEvent('custom-click'));");
        Console.WriteLine("    });");
        Console.WriteLine("  }");
        Console.WriteLine("}");
        Console.WriteLine("customElements.define('custom-button', CustomButton);");
        
        Console.WriteLine("\n// Modern focus management");
        Console.WriteLine("function trapFocus(container) {");
        Console.WriteLine("  const focusableElements = container.querySelectorAll(");
        Console.WriteLine("    'a[href], button, textarea, input, select, [tabindex]:not([tabindex=\"-1\"])'");
        Console.WriteLine("  );");
        Console.WriteLine("  ");
        Console.WriteLine("  const firstElement = focusableElements[0];");
        Console.WriteLine("  const lastElement = focusableElements[focusableElements.length - 1];");
        Console.WriteLine("  ");
        Console.WriteLine("  container.addEventListener('keydown', (e) => {");
        Console.WriteLine("    if (e.key === 'Tab') {");
        Console.WriteLine("      if (e.shiftKey) {");
        Console.WriteLine("        if (document.activeElement === firstElement) {");
        Console.WriteLine("          e.preventDefault();");
        Console.WriteLine("          lastElement.focus();");
        Console.WriteLine("        }");
        Console.WriteLine("      } else {");
        Console.WriteLine("        if (document.activeElement === lastElement) {");
        Console.WriteLine("          e.preventDefault();");
        Console.WriteLine("          firstElement.focus();");
        Console.WriteLine("        }");
        Console.WriteLine("      }");
        Console.WriteLine("    }");
        Console.WriteLine("  });");
        Console.WriteLine("}");
    }
}