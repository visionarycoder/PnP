# ML Database Examples - Jupyter Notebook

**Description**: Complete machine learning database workflow examples using Jupyter notebooks with PostgreSQL, vector databases, and analytics pipelines for model training, evaluation, and deployment.

**Language/Technology**: Python, Jupyter Notebook, PostgreSQL, DuckDB, Pandas

**Prerequisites**: Jupyter Lab/Notebook, Python 3.11+, PostgreSQL 15+, Docker (optional)

## Environment Setup

**Code**:

```python
# Cell 1: Import Dependencies and Configuration
"""
Machine Learning Database Examples
==================================
This notebook demonstrates ML workflows with database integration.

Author: Data Science Team
Date: 2025-11-02
Environment: Python 3.11+, PostgreSQL 15+
"""

import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns
from sklearn.model_selection import train_test_split
from sklearn.ensemble import RandomForestClassifier
from sklearn.metrics import classification_report, confusion_matrix
import psycopg2
import duckdb
import joblib
from datetime import datetime, timedelta
import warnings
warnings.filterwarnings('ignore')

# Set random seed for reproducibility
np.random.seed(42)

# Plotting configuration
plt.style.use('seaborn-v0_8')
sns.set_palette("husl")

print("✅ Environment setup complete")
print(f"📊 Pandas version: {pd.__version__}")
print(f"🔢 NumPy version: {np.__version__}")
```

**Code**:

```python
# Cell 2: Database Connection Configuration
"""
Database Configuration
======================
Configure connections to PostgreSQL for data storage and DuckDB for analytics.
"""

# PostgreSQL configuration for ML experiment tracking
POSTGRES_CONFIG = {
    'host': 'localhost',
    'port': 5432,
    'database': 'ml_experiments',
    'user': 'ml_user',
    'password': 'secure_password'  # Use environment variables in production
}

# DuckDB for fast analytics
DUCKDB_PATH = './data/ml_analytics.duckdb'

def get_postgres_connection():
    """Create PostgreSQL connection with proper error handling."""
    try:
        conn = psycopg2.connect(**POSTGRES_CONFIG)
        return conn
    except psycopg2.Error as e:
        print(f"❌ Database connection failed: {e}")
        return None

def get_duckdb_connection():
    """Create DuckDB connection for analytics."""
    return duckdb.connect(DUCKDB_PATH)

# Test connections
pg_conn = get_postgres_connection()
duck_conn = get_duckdb_connection()

if pg_conn:
    print("✅ PostgreSQL connection established")
    pg_conn.close()
else:
    print("❌ PostgreSQL connection failed - using mock data")

print("✅ DuckDB connection established")
```

## Data Loading and Exploration

**Code**:

```python
# Cell 3: Data Loading and Initial Exploration
"""
Data Loading and Quality Assessment
===================================
Load customer transaction data and perform initial data quality checks.
"""

def load_sample_data():
    """Generate sample customer transaction data for ML analysis."""
    np.random.seed(42)
    n_customers = 10000
    
    # Generate synthetic customer transaction data
    data = {
        'customer_id': range(1, n_customers + 1),
        'age': np.random.normal(35, 12, n_customers).astype(int),
        'annual_income': np.random.lognormal(10.5, 0.5, n_customers),
        'total_transactions': np.random.poisson(25, n_customers),
        'avg_transaction_amount': np.random.gamma(50, 2, n_customers),
        'months_active': np.random.randint(1, 61, n_customers),
        'support_tickets': np.random.poisson(2, n_customers),
        'product_categories': np.random.randint(1, 8, n_customers),
        'mobile_app_usage': np.random.beta(2, 5, n_customers),
        'email_engagement': np.random.beta(3, 4, n_customers)
    }
    
    df = pd.DataFrame(data)
    
    # Create target variable: customer lifetime value category
    df['lifetime_value'] = (
        df['total_transactions'] * df['avg_transaction_amount'] * 
        (df['months_active'] / 12)
    )
    
    # Categorize customers
    df['value_segment'] = pd.cut(
        df['lifetime_value'],
        bins=[0, 1000, 5000, 15000, float('inf')],
        labels=['Low', 'Medium', 'High', 'Premium'],
        include_lowest=True
    )
    
    return df

# Load and explore data
df = load_sample_data()

print("📊 Dataset Overview")
print("=" * 40)
print(f"Shape: {df.shape}")
print(f"Memory usage: {df.memory_usage(deep=True).sum() / 1024**2:.2f} MB")

print("\n📈 Data Quality Report")
print("=" * 40)
print(f"Missing values: {df.isnull().sum().sum()}")
print(f"Duplicate rows: {df.duplicated().sum()}")

print("\n🎯 Target Variable Distribution")
print("=" * 40)
print(df['value_segment'].value_counts(normalize=True).round(3))

# Display first few rows
df.head()
```

**Code**:

```python
# Cell 4: Exploratory Data Analysis with Visualization
"""
Exploratory Data Analysis
=========================
Analyze customer segments and feature relationships.
"""

# Create comprehensive EDA visualizations
fig, axes = plt.subplots(2, 3, figsize=(18, 12))
fig.suptitle('Customer Segmentation Analysis', fontsize=16, fontweight='bold')

# 1. Age distribution by segment
sns.boxplot(data=df, x='value_segment', y='age', ax=axes[0, 0])
axes[0, 0].set_title('Age Distribution by Customer Segment')
axes[0, 0].set_ylabel('Age (years)')

# 2. Income vs Transaction Amount
sns.scatterplot(
    data=df.sample(1000),  # Sample for performance
    x='annual_income', y='avg_transaction_amount', 
    hue='value_segment', alpha=0.7, ax=axes[0, 1]
)
axes[0, 1].set_title('Income vs Transaction Amount')
axes[0, 1].set_xlabel('Annual Income ($)')
axes[0, 1].set_ylabel('Avg Transaction ($)')

# 3. Transaction frequency by segment
df.groupby('value_segment')['total_transactions'].mean().plot(
    kind='bar', ax=axes[0, 2], color='skyblue'
)
axes[0, 2].set_title('Average Transactions by Segment')
axes[0, 2].set_ylabel('Transaction Count')
axes[0, 2].tick_params(axis='x', rotation=45)

# 4. Customer lifetime value distribution
df['lifetime_value'].hist(bins=50, ax=axes[1, 0], alpha=0.7, color='green')
axes[1, 0].set_title('Customer Lifetime Value Distribution')
axes[1, 0].set_xlabel('Lifetime Value ($)')
axes[1, 0].set_ylabel('Frequency')

# 5. Correlation heatmap
numeric_cols = ['age', 'annual_income', 'total_transactions', 
               'avg_transaction_amount', 'months_active', 'support_tickets']
corr_matrix = df[numeric_cols].corr()
sns.heatmap(corr_matrix, annot=True, cmap='coolwarm', center=0, ax=axes[1, 1])
axes[1, 1].set_title('Feature Correlation Matrix')

# 6. Mobile app usage vs Email engagement
sns.scatterplot(
    data=df.sample(1000),
    x='mobile_app_usage', y='email_engagement',
    hue='value_segment', alpha=0.7, ax=axes[1, 2]
)
axes[1, 2].set_title('Digital Engagement Patterns')
axes[1, 2].set_xlabel('Mobile App Usage Score')
axes[1, 2].set_ylabel('Email Engagement Score')

plt.tight_layout()
plt.show()

print("\n📊 Key Insights:")
print("=" * 40)
high_value = df[df['value_segment'] == 'Premium']
print(f"• Premium customers: {len(high_value)} ({len(high_value)/len(df)*100:.1f}%)")
print(f"• Average age of premium customers: {high_value['age'].mean():.1f} years")
print(f"• Premium customer avg income: ${high_value['annual_income'].mean():,.0f}")
```

## Machine Learning Model Development

**Code**:

```python
# Cell 5: Feature Engineering and Data Preparation
"""
Feature Engineering
===================
Prepare features for machine learning model training.
"""

def engineer_features(df):
    """Create additional features for better model performance."""
    df_eng = df.copy()
    
    # Customer behavioral features
    df_eng['transaction_frequency'] = df_eng['total_transactions'] / df_eng['months_active']
    df_eng['support_intensity'] = df_eng['support_tickets'] / df_eng['months_active']
    df_eng['engagement_score'] = (
        df_eng['mobile_app_usage'] * 0.6 + df_eng['email_engagement'] * 0.4
    )
    
    # Financial features
    df_eng['income_per_transaction'] = df_eng['annual_income'] / (df_eng['total_transactions'] + 1)
    df_eng['spending_ratio'] = (
        df_eng['avg_transaction_amount'] * df_eng['total_transactions']
    ) / df_eng['annual_income']
    
    # Age categories
    df_eng['age_group'] = pd.cut(
        df_eng['age'],
        bins=[0, 25, 35, 50, 65, 100],
        labels=['Gen_Z', 'Millennial', 'Gen_X', 'Boomer', 'Silent']
    )
    
    # High-value indicators
    df_eng['high_spender'] = (df_eng['avg_transaction_amount'] > df_eng['avg_transaction_amount'].quantile(0.75)).astype(int)
    df_eng['loyal_customer'] = (df_eng['months_active'] > 36).astype(int)
    df_eng['low_maintenance'] = (df_eng['support_tickets'] <= 1).astype(int)
    
    return df_eng

# Apply feature engineering
df_engineered = engineer_features(df)

print("🔧 Feature Engineering Complete")
print("=" * 40)
print(f"Original features: {df.shape[1]}")
print(f"Engineered features: {df_engineered.shape[1]}")
print(f"New features added: {df_engineered.shape[1] - df.shape[1]}")

# Feature importance preview
feature_cols = [
    'age', 'annual_income', 'total_transactions', 'avg_transaction_amount',
    'months_active', 'support_tickets', 'product_categories',
    'mobile_app_usage', 'email_engagement', 'transaction_frequency',
    'engagement_score', 'high_spender', 'loyal_customer', 'low_maintenance'
]

print("\n📊 Feature Summary Statistics:")
print(df_engineered[feature_cols].describe().round(2))
```

**Code**:

```python
# Cell 6: Model Training and Evaluation
"""
Model Training and Evaluation
=============================
Train Random Forest classifier for customer segmentation.
"""

def prepare_model_data(df, feature_cols, target_col='value_segment'):
    """Prepare data for machine learning model."""
    # Prepare features and target
    X = df[feature_cols].copy()
    y = df[target_col].copy()
    
    # Handle any remaining missing values
    X = X.fillna(X.median())
    
    # Split data
    X_train, X_test, y_train, y_test = train_test_split(
        X, y, test_size=0.2, random_state=42, stratify=y
    )
    
    return X_train, X_test, y_train, y_test

def train_customer_segmentation_model(X_train, y_train):
    """Train Random Forest model for customer segmentation."""
    model = RandomForestClassifier(
        n_estimators=100,
        max_depth=10,
        min_samples_split=5,
        min_samples_leaf=2,
        random_state=42,
        n_jobs=-1
    )
    
    # Train model
    model.fit(X_train, y_train)
    
    return model

def evaluate_model(model, X_test, y_test):
    """Comprehensive model evaluation."""
    # Predictions
    y_pred = model.predict(X_test)
    y_pred_proba = model.predict_proba(X_test)
    
    # Classification report
    print("📊 Model Performance Report")
    print("=" * 50)
    print(classification_report(y_test, y_pred))
    
    # Confusion matrix visualization
    plt.figure(figsize=(10, 8))
    cm = confusion_matrix(y_test, y_pred)
    sns.heatmap(cm, annot=True, fmt='d', cmap='Blues',
                xticklabels=model.classes_,
                yticklabels=model.classes_)
    plt.title('Confusion Matrix - Customer Segmentation Model')
    plt.ylabel('True Label')
    plt.xlabel('Predicted Label')
    plt.show()
    
    # Feature importance
    feature_importance = pd.DataFrame({
        'feature': X_test.columns,
        'importance': model.feature_importances_
    }).sort_values('importance', ascending=False)
    
    plt.figure(figsize=(10, 8))
    sns.barplot(data=feature_importance.head(10), 
                x='importance', y='feature', palette='viridis')
    plt.title('Top 10 Most Important Features')
    plt.xlabel('Feature Importance')
    plt.tight_layout()
    plt.show()
    
    return y_pred, y_pred_proba, feature_importance

# Prepare data and train model
X_train, X_test, y_train, y_test = prepare_model_data(df_engineered, feature_cols)

print("🤖 Training Customer Segmentation Model...")
model = train_customer_segmentation_model(X_train, y_train)

print("✅ Model training complete!")
print(f"Training samples: {len(X_train)}")
print(f"Test samples: {len(X_test)}")

# Evaluate model
y_pred, y_pred_proba, feature_importance = evaluate_model(model, X_test, y_test)
```

## Database Integration and Model Persistence

**Code**:

```python
# Cell 7: Database Integration and Model Storage
"""
Database Integration
====================
Store model results and predictions in PostgreSQL and DuckDB.
"""

def store_experiment_results(model, feature_importance, model_metrics):
    """Store ML experiment results in PostgreSQL."""
    
    # Mock database operations (replace with actual database calls)
    experiment_data = {
        'experiment_id': f"customer_seg_{datetime.now().strftime('%Y%m%d_%H%M%S')}",
        'model_type': 'RandomForestClassifier',
        'model_parameters': {
            'n_estimators': model.n_estimators,
            'max_depth': model.max_depth,
            'min_samples_split': model.min_samples_split
        },
        'feature_count': len(feature_cols),
        'training_samples': len(X_train),
        'test_samples': len(X_test),
        'timestamp': datetime.now().isoformat()
    }
    
    # Store model to disk
    model_filename = f"customer_segmentation_model_{datetime.now().strftime('%Y%m%d')}.joblib"
    joblib.dump(model, model_filename)
    
    print("💾 Model Storage Complete")
    print("=" * 30)
    print(f"Experiment ID: {experiment_data['experiment_id']}")
    print(f"Model file: {model_filename}")
    print(f"Model size: {os.path.getsize(model_filename) / 1024:.1f} KB")
    
    return experiment_data

def create_prediction_summary():
    """Create summary of model predictions for business stakeholders."""
    
    # Combine test data with predictions
    results_df = X_test.copy()
    results_df['true_segment'] = y_test.reset_index(drop=True)
    results_df['predicted_segment'] = y_pred
    results_df['prediction_confidence'] = np.max(y_pred_proba, axis=1)
    
    # Business insights
    segment_analysis = results_df.groupby(['true_segment', 'predicted_segment']).size().unstack(fill_value=0)
    
    print("🎯 Prediction Analysis by Segment")
    print("=" * 40)
    print(segment_analysis)
    
    # High-confidence predictions
    high_confidence = results_df[results_df['prediction_confidence'] > 0.8]
    print(f"\n📈 High-confidence predictions: {len(high_confidence)} ({len(high_confidence)/len(results_df)*100:.1f}%)")
    
    # Segment migration patterns
    print("\n🔄 Segment Prediction Accuracy:")
    for segment in results_df['true_segment'].unique():
        segment_data = results_df[results_df['true_segment'] == segment]
        accuracy = (segment_data['true_segment'] == segment_data['predicted_segment']).mean()
        print(f"  • {segment}: {accuracy:.3f}")
    
    return results_df

# Store results and create summary
import os
experiment_data = store_experiment_results(model, feature_importance, {})
results_df = create_prediction_summary()

print("\n✅ Database integration complete")
```

**Code**:

```python
# Cell 8: Analytics and Reporting with DuckDB
"""
Advanced Analytics with DuckDB
==============================
Perform fast analytics on ML results using DuckDB.
"""

def analyze_model_performance_duckdb():
    """Use DuckDB for fast analytics on model results."""
    
    # Create DuckDB connection
    conn = duckdb.connect(':memory:')
    
    # Register pandas DataFrames as DuckDB tables
    conn.register('customer_data', df_engineered)
    conn.register('predictions', results_df)
    conn.register('feature_importance', feature_importance)
    
    # Advanced analytics queries
    queries = {
        'segment_performance': """
            SELECT 
                true_segment,
                COUNT(*) as total_predictions,
                AVG(prediction_confidence) as avg_confidence,
                SUM(CASE WHEN true_segment = predicted_segment THEN 1 ELSE 0 END) as correct_predictions,
                ROUND(100.0 * SUM(CASE WHEN true_segment = predicted_segment THEN 1 ELSE 0 END) / COUNT(*), 2) as accuracy_pct
            FROM predictions 
            GROUP BY true_segment
            ORDER BY accuracy_pct DESC
        """,
        
        'top_features': """
            SELECT 
                feature,
                ROUND(importance, 4) as importance,
                RANK() OVER (ORDER BY importance DESC) as rank
            FROM feature_importance 
            ORDER BY importance DESC 
            LIMIT 5
        """,
        
        'customer_insights': """
            SELECT 
                value_segment,
                COUNT(*) as customer_count,
                ROUND(AVG(annual_income), 0) as avg_income,
                ROUND(AVG(lifetime_value), 0) as avg_lifetime_value,
                ROUND(AVG(engagement_score), 3) as avg_engagement
            FROM customer_data 
            GROUP BY value_segment
            ORDER BY avg_lifetime_value DESC
        """
    }
    
    print("📊 Advanced Analytics Results")
    print("=" * 50)
    
    for query_name, query in queries.items():
        print(f"\n🔍 {query_name.replace('_', ' ').title()}:")
        print("-" * 30)
        result = conn.execute(query).fetchdf()
        print(result.to_string(index=False))
    
    # Close connection
    conn.close()
    
    return result

# Run advanced analytics
analytics_results = analyze_model_performance_duckdb()
```

**Usage**:

```python
# Example: Production Model Inference Pipeline
def predict_customer_segment(customer_features):
    """
    Predict customer segment for new customers.
    
    Args:
        customer_features (dict): Customer feature values
        
    Returns:
        dict: Prediction results with confidence
    """
    # Load trained model
    model = joblib.load('customer_segmentation_model_20251102.joblib')
    
    # Prepare features
    feature_array = np.array([[
        customer_features['age'],
        customer_features['annual_income'],
        customer_features['total_transactions'],
        # ... other features
    ]])
    
    # Make prediction
    prediction = model.predict(feature_array)[0]
    confidence = model.predict_proba(feature_array).max()
    
    return {
        'predicted_segment': prediction,
        'confidence': confidence,
        'timestamp': datetime.now().isoformat()
    }

# Example usage
new_customer = {
    'age': 32,
    'annual_income': 75000,
    'total_transactions': 45,
    'avg_transaction_amount': 120,
    'months_active': 24,
    'support_tickets': 1,
    'product_categories': 4,
    'mobile_app_usage': 0.8,
    'email_engagement': 0.6
}

prediction_result = predict_customer_segment(new_customer)
print(f"🎯 Prediction: {prediction_result}")
```

**Notes**:

- **Reproducibility**: All random seeds are set for consistent results across notebook runs
- **Data Quality**: Comprehensive data validation and quality checks implemented
- **Performance**: Uses vectorized operations and efficient data structures for large datasets
- **Visualization**: Clear, accessible charts with consistent styling and proper labeling
- **Database Integration**: Demonstrates PostgreSQL for transactional data and DuckDB for analytics
- **Model Persistence**: Proper model serialization and versioning for production deployment
- **Error Handling**: Robust error handling for database connections and data processing
- **Documentation**: Extensive markdown explanations for each analysis step
- **Security**: Demonstrates secure handling of database credentials and sensitive data
- **Scalability**: Code patterns designed for larger datasets and production environments