---
description: Jupyter notebook development and data science best practices
applyTo: '**/*.ipynb,**/notebooks/**,**/*notebook*'
---

# Notebooks Instructions

## Scope
Applies to Jupyter notebooks, data science workflows, and interactive computing.

## Notebook Organization
- Use clear, descriptive notebook names with dates when appropriate.
- Structure notebooks with logical sections and clear headings.
- Use markdown cells to explain analysis steps and findings.
- Keep notebooks focused on single analysis or experiment.
- Use table of contents for longer notebooks.

## Code Cell Best Practices
- Keep code cells focused and modular.
- Use functions for reusable code blocks.
- Import all dependencies at the top of the notebook.
- Use descriptive variable names and comments.
- Avoid very long code cells that are hard to debug.

## Data Handling
- Load data early and validate structure and quality.
- Use appropriate data types for memory efficiency.
- Handle missing data explicitly with documented strategies.
- Create data quality checks and validation steps.
- Use version control for datasets when possible.

## Visualization Standards
- Choose appropriate chart types for data and message.
- Use consistent styling and color schemes.
- Include clear titles, labels, and legends.
- Make visualizations accessible with proper contrast.
- Export important plots for use in reports or presentations.

## Documentation and Narrative
- Use markdown cells to explain analysis rationale.
- Document assumptions and methodological choices.
- Include interpretation of results and findings.
- Use proper markdown formatting for readability.
- Include references to data sources and methodologies.

## Version Control Integration
- Clear output before committing to version control.
- Use .gitignore for large data files and temporary outputs.
- Include environment specification files (requirements.txt, environment.yml).
- Use meaningful commit messages for notebook changes.
- Consider using notebook diff tools for better version control.

## Reproducibility Practices
- Pin package versions in environment specifications.
- Set random seeds for reproducible results.
- Document system requirements and dependencies.
- Use relative paths for data file references.
- Include data provenance and collection information.

## Performance Optimization
- Use vectorized operations instead of loops when possible.
- Profile code performance for computational bottlenecks.
- Use appropriate data structures for memory efficiency.
- Consider using lazy loading for large datasets.
- Clear unnecessary variables to free memory.

## Testing and Validation
- Include data validation and sanity checks.
- Test functions and analysis logic with sample data.
- Validate results against known benchmarks or expectations.
- Cross-validate machine learning models appropriately.
- Document and test edge cases and error conditions.

## Security and Privacy
- Avoid hardcoding sensitive information (passwords, API keys).
- Use environment variables or configuration files for secrets.
- Be cautious with data containing personal information.
- Follow data privacy regulations and organizational policies.
- Sanitize outputs before sharing notebooks publicly.

## Collaboration Guidelines
- Use clear section headers and explanatory text.
- Include setup instructions for running the notebook.
- Document required data files and their locations.
- Use consistent coding style within teams.
- Include contact information for questions or clarifications.

## Production Deployment
- Convert critical analysis to production scripts when needed.
- Use parameter cells for configurable notebook execution.
- Implement proper error handling for automated runs.
- Test notebooks in clean environments before deployment.
- Document deployment requirements and procedures.