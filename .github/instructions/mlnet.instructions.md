---
description: ML.NET machine learning development best practices
applyTo: '**/mlnet/**,**/*.mlnet.*,**/*machinelearning*,**/*ml*'
---

# ML.NET Instructions

## Scope
Applies to ML.NET machine learning model development, training, and deployment.

## Model Development Workflow
- Start with data exploration and quality assessment.
- Choose appropriate ML task type (classification, regression, etc.).
- Split data into training, validation, and test sets properly.
- Use cross-validation for robust model evaluation.
- Document data preprocessing steps and transformations.

## Data Pipeline Design
- Use ML.NET's pipeline API for reproducible transformations.
- Handle missing data with appropriate strategies.
- Implement proper feature engineering and selection.
- Use appropriate data loading and caching strategies.
- Validate data quality and distribution consistency.

## Model Training Best Practices
- Use appropriate algorithms for your data and problem type.
- Implement proper hyperparameter tuning strategies.
- Monitor training progress and implement early stopping.
- Use appropriate evaluation metrics for your problem domain.
- Save model training metadata for reproducibility.

## Performance Optimization
- Use appropriate data types and memory-efficient operations.
- Implement batch processing for large datasets.
- Use GPU acceleration when available and beneficial.
- Profile memory usage and optimize data loading.
- Consider model compression techniques for deployment.

## Model Evaluation and Validation
- Use appropriate metrics for your ML task type.
- Implement cross-validation with proper stratification.
- Test model performance on held-out test data.
- Analyze model predictions for bias and fairness.
- Document model limitations and expected performance.

## Feature Engineering
- Create domain-specific features based on business knowledge.
- Use appropriate encoding for categorical variables.
- Implement proper scaling and normalization.
- Handle temporal features and seasonality appropriately.
- Document feature importance and selection rationale.

## Model Deployment and Serving
- Use MLContext for consistent model loading and prediction.
- Implement proper error handling for prediction scenarios.
- Use appropriate serialization for model persistence.
- Monitor model performance in production.
- Implement A/B testing for model comparisons.

## Data Management
- Implement proper data versioning and lineage tracking.
- Use appropriate data storage formats (Parquet, CSV, etc.).
- Handle data schema evolution and backward compatibility.
- Implement data quality monitoring and validation.
- Use proper data governance and privacy practices.

## Testing Strategies
- Unit test data transformation and preprocessing logic.
- Integration test complete ML pipelines.
- Test model predictions with known data samples.
- Validate model serialization and deserialization.
- Performance test training and prediction scenarios.

## AutoML Integration
- Use AutoML for baseline model development and comparison.
- Understand AutoML-generated pipeline components.
- Customize AutoML results for specific requirements.
- Validate AutoML model performance thoroughly.
- Document AutoML configuration and results.

## Production Monitoring
- Monitor model prediction accuracy over time.
- Detect data drift in input features.
- Implement proper logging for model predictions.
- Set up alerting for model performance degradation.
- Plan for model retraining and updating strategies.

## Ethical AI Considerations
- Test models for bias across different demographic groups.
- Implement explainability features for model decisions.
- Document model assumptions and limitations clearly.
- Consider fairness metrics in model evaluation.
- Implement proper consent and privacy protection.