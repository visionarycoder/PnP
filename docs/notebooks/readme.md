# ML Database Examples

**Description**: Interactive examples demonstrating machine learning database workflows using PostgreSQL+pgvector, Chroma, and DuckDB  
**Language/Technology**: Python, PostgreSQL, DuckDB, Chroma  
**Category**: Machine Learning, Databases, Analytics

## Overview

This example demonstrates a complete ML workflow integrating multiple database technologies:

- **PostgreSQL + pgvector**: Structured experiment tracking with vector similarity search
- **Chroma**: Dedicated vector database for document embeddings and retrieval
- **DuckDB**: High-performance analytics and reporting on experiment results

## Setup and Configuration

### Required Dependencies

```python
import psycopg2
import requests
import duckdb
import pandas as pd
import numpy as np
import json
from datetime import datetime
import matplotlib.pyplot as plt
import seaborn as sns
```

### Database Configuration

```python
# PostgreSQL Configuration
POSTGRES_CONFIG = {
    'host': 'localhost',
    'port': 5432,
    'database': 'ml_examples',
    'user': 'ml_user', 
    'password': 'ml_pass123'
}

# Chroma Vector Database
CHROMA_BASE_URL = 'http://localhost:8000'

# DuckDB Analytics Database
DUCKDB_PATH = './data/duckdb/ml_analytics.duckdb'
```

## 1. PostgreSQL + pgvector Examples

### Experiment Tracking

```python
def get_postgres_connection():
    return psycopg2.connect(**POSTGRES_CONFIG)

def create_experiment():
    conn = get_postgres_connection()
    cur = conn.cursor()
    
    experiment_data = {
        'name': 'Text Classification v2.0',
        'model_type': 'TransformerModel',
        'description': 'BERT-based sentiment analysis',
        'parameters': json.dumps({
            'learning_rate': 2e-5,
            'batch_size': 16,
            'epochs': 3,
            'warmup_steps': 500
        })
    }
    
    cur.execute("""
        INSERT INTO ml_schema.experiments (name, model_type, description, parameters)
        VALUES (%(name)s, %(model_type)s, %(description)s, %(parameters)s)
        RETURNING id;
    """, experiment_data)
    
    experiment_id = cur.fetchone()[0]
    conn.commit()
    cur.close()
    conn.close()
    
    print(f"Created experiment: {experiment_id}")
    return experiment_id
```

### Document Embeddings Storage

```python
def store_embeddings():
    conn = get_postgres_connection()
    cur = conn.cursor()
    
    # Sample documents and their embeddings
    documents = [
        {
            'document_id': 'review_001',
            'content': 'This product is amazing! Great quality and fast shipping.',
            'embedding': np.random.normal(0, 0.1, 1536).tolist()
        },
        {
            'document_id': 'review_002', 
            'content': 'Poor quality product. Very disappointed with purchase.',
            'embedding': np.random.normal(0, 0.1, 1536).tolist()
        },
        {
            'document_id': 'review_003',
            'content': 'Average product. Nothing special but does the job.',
            'embedding': np.random.normal(0, 0.1, 1536).tolist()
        }
    ]
    
    for doc in documents:
        cur.execute("""
            INSERT INTO ml_schema.document_embeddings 
            (document_id, content_hash, embedding, model_name, metadata)
            VALUES (%s, %s, %s, %s, %s);
        """, (
            doc['document_id'],
            hash(doc['content']),
            doc['embedding'],
            'text-embedding-ada-002',
            json.dumps({'content': doc['content'], 'type': 'product_review'})
        ))
    
    conn.commit()
    cur.close()
    conn.close()
    
    print(f"Stored {len(documents)} embeddings")
```

### Vector Similarity Search

```python
def find_similar_documents(query_embedding, limit=5):
    conn = get_postgres_connection()
    cur = conn.cursor()
    
    cur.execute("""
        SELECT 
            document_id,
            model_name,
            metadata,
            embedding <=> %s as distance
        FROM ml_schema.document_embeddings
        ORDER BY distance
        LIMIT %s;
    """, (query_embedding, limit))
    
    results = cur.fetchall()
    cur.close()
    conn.close()
    
    return results

# Usage Example
query_embedding = np.random.normal(0, 0.1, 1536).tolist()
similar_docs = find_similar_documents(query_embedding)

for doc_id, model_name, metadata, distance in similar_docs:
    print(f"Document: {doc_id}, Distance: {distance:.4f}")
    print(f"Content: {json.loads(metadata)['content'][:50]}...")
```

## 2. Chroma Vector Database Examples

### Collection Management

```python
def create_chroma_collection(name, metadata=None):
    response = requests.post(
        f"{CHROMA_BASE_URL}/api/v1/collections",
        json={
            'name': name,
            'metadata': metadata or {}
        }
    )
    
    if response.status_code == 200:
        print(f"Created collection: {name}")
        return response.json()
    else:
        print(f"Error creating collection: {response.text}")
        return None

def add_documents_to_chroma(collection_name, documents):
    """Add documents with embeddings to Chroma collection"""
    response = requests.post(
        f"{CHROMA_BASE_URL}/api/v1/collections/{collection_name}/add",
        json={
            'ids': [doc['id'] for doc in documents],
            'embeddings': [doc['embedding'] for doc in documents],
            'documents': [doc['content'] for doc in documents],
            'metadatas': [doc.get('metadata', {}) for doc in documents]
        }
    )
    
    if response.status_code == 200:
        print(f"Added {len(documents)} documents to {collection_name}")
        return True
    else:
        print(f"Error adding documents: {response.text}")
        return False
```

### Document Query and Retrieval

```python
def query_chroma_collection(collection_name, query_embedding, n_results=5):
    """Query Chroma collection for similar documents"""
    response = requests.post(
        f"{CHROMA_BASE_URL}/api/v1/collections/{collection_name}/query",
        json={
            'query_embeddings': [query_embedding],
            'n_results': n_results,
            'include': ['metadatas', 'documents', 'distances']
        }
    )
    
    if response.status_code == 200:
        return response.json()
    else:
        print(f"Error querying collection: {response.text}")
        return None

# Usage Example
collection_name = "product_reviews"
create_chroma_collection(collection_name, {
    'description': 'Product review embeddings',
    'model': 'text-embedding-ada-002'
})

sample_documents = [
    {
        'id': 'review_001',
        'content': 'Excellent product quality, fast delivery, highly recommended!',
        'embedding': np.random.normal(0, 0.1, 384).tolist(),
        'metadata': {'rating': 5, 'verified_purchase': True, 'category': 'electronics'}
    },
    {
        'id': 'review_002', 
        'content': 'Product arrived damaged, poor packaging quality.',
        'embedding': np.random.normal(0, 0.1, 384).tolist(),
        'metadata': {'rating': 1, 'verified_purchase': True, 'category': 'electronics'}
    }
]

add_documents_to_chroma(collection_name, sample_documents)
```

## 3. DuckDB Analytics Examples

### Performance Analysis Setup

```python
def setup_duckdb():
    """Initialize DuckDB with sample data"""
    conn = duckdb.connect(DUCKDB_PATH)
    
    # Create sample experiment data
    conn.execute("""
        CREATE TABLE IF NOT EXISTS experiments_performance AS
        SELECT * FROM VALUES
            ('2024-01-15'::DATE, 'RandomForest', 'sentiment_v1', 0.85, 0.83, 0.87, 120, 15.5),
            ('2024-01-15'::DATE, 'XGBoost', 'sentiment_v1', 0.88, 0.86, 0.90, 180, 12.3),
            ('2024-01-15'::DATE, 'BERT', 'sentiment_v1', 0.92, 0.91, 0.93, 3600, 45.2),
            ('2024-01-16'::DATE, 'RandomForest', 'sentiment_v2', 0.87, 0.85, 0.89, 125, 14.8),
            ('2024-01-16'::DATE, 'XGBoost', 'sentiment_v2', 0.90, 0.88, 0.92, 175, 11.9),
            ('2024-01-16'::DATE, 'BERT', 'sentiment_v2', 0.94, 0.93, 0.95, 3400, 42.1)
        AS t(date, model_name, dataset, accuracy, precision, recall, training_time_sec, inference_time_ms);
    """)
    
    print("DuckDB initialized with sample data")
    return conn

def analyze_model_performance(conn):
    """Analyze and visualize model performance"""
    
    # Query performance data
    df = conn.execute("""
        SELECT 
            model_name,
            AVG(accuracy) as avg_accuracy,
            AVG(training_time_sec) as avg_training_time,
            AVG(inference_time_ms) as avg_inference_time,
            COUNT(*) as experiment_count
        FROM experiments_performance 
        GROUP BY model_name
        ORDER BY avg_accuracy DESC;
    """).df()
    
    print("Model Performance Comparison:")
    print(df.to_string(index=False))
    
    return df
```

### Visualization and Reporting

```python
def create_performance_charts(df):
    fig, axes = plt.subplots(2, 2, figsize=(12, 8))
    
    # Accuracy comparison
    axes[0,0].bar(df['model_name'], df['avg_accuracy'])
    axes[0,0].set_title('Average Accuracy by Model')
    axes[0,0].set_ylabel('Accuracy')
    axes[0,0].tick_params(axis='x', rotation=45)
    
    # Training time comparison
    axes[0,1].bar(df['model_name'], df['avg_training_time'])
    axes[0,1].set_title('Average Training Time by Model')
    axes[0,1].set_ylabel('Training Time (seconds)')
    axes[0,1].tick_params(axis='x', rotation=45)
    
    # Inference time comparison  
    axes[1,0].bar(df['model_name'], df['avg_inference_time'])
    axes[1,0].set_title('Average Inference Time by Model')
    axes[1,0].set_ylabel('Inference Time (ms)')
    axes[1,0].tick_params(axis='x', rotation=45)
    
    # Accuracy vs Inference Time scatter plot
    axes[1,1].scatter(df['avg_inference_time'], df['avg_accuracy'])
    for i, model in enumerate(df['model_name']):
        axes[1,1].annotate(model, (df['avg_inference_time'].iloc[i], df['avg_accuracy'].iloc[i]))
    axes[1,1].set_xlabel('Inference Time (ms)')
    axes[1,1].set_ylabel('Accuracy')
    axes[1,1].set_title('Accuracy vs Inference Time')
    
    plt.tight_layout()
    plt.show()
```

## 4. Integrated ML Workflow

### Complete Workflow Manager

```python
class MLWorkflowManager:
    def __init__(self):
        self.postgres_config = POSTGRES_CONFIG
        self.chroma_base_url = CHROMA_BASE_URL
        self.duckdb_path = DUCKDB_PATH
    
    def run_ml_experiment(self, model_name, dataset_name, model_params):
        """Run a complete ML experiment workflow"""
        
        print(f"Starting ML experiment: {model_name} on {dataset_name}")
        
        # 1. Create experiment in PostgreSQL
        experiment_id = self._create_postgres_experiment(model_name, dataset_name, model_params)
        
        # 2. Store model embeddings in Chroma
        collection_name = f"{model_name.lower()}_{dataset_name.lower()}"
        self._store_embeddings_chroma(collection_name, model_name)
        
        # 3. Simulate training and get results
        results = self._simulate_training(model_name)
        
        # 4. Store performance metrics in DuckDB
        self._store_performance_duckdb(experiment_id, model_name, dataset_name, results)
        
        # 5. Update experiment status in PostgreSQL
        self._update_postgres_experiment(experiment_id, results)
        
        print(f"Experiment {experiment_id} completed successfully!")
        return experiment_id, results
    
    def _simulate_training(self, model_name):
        """Simulate training and return performance metrics"""
        
        # Different models have different characteristics
        base_metrics = {
            'RandomForest': {'accuracy': 0.85, 'training_time': 120, 'inference_time': 15},
            'XGBoost': {'accuracy': 0.88, 'training_time': 180, 'inference_time': 12},
            'NeuralNet': {'accuracy': 0.91, 'training_time': 450, 'inference_time': 25}
        }
        
        base = base_metrics.get(model_name, {'accuracy': 0.80, 'training_time': 200, 'inference_time': 20})
        
        # Add some random variation
        results = {
            'accuracy': base['accuracy'] + np.random.normal(0, 0.02),
            'precision': base['accuracy'] + np.random.normal(0, 0.015),
            'recall': base['accuracy'] + np.random.normal(0, 0.015),
            'f1_score': base['accuracy'] + np.random.normal(0, 0.01),
            'training_time_seconds': int(base['training_time'] + np.random.normal(0, 20)),
            'inference_time_ms': base['inference_time'] + np.random.normal(0, 2),
            'memory_usage_mb': np.random.uniform(200, 600)
        }
        
        return results
```

### Running Multiple Experiments

```python
# Usage Example
workflow = MLWorkflowManager()

models_to_test = ['RandomForest', 'XGBoost', 'NeuralNet']
dataset = 'sentiment_analysis_v3'

experiment_results = []

for model in models_to_test:
    params = {
        'learning_rate': 0.001,
        'batch_size': 32,
        'regularization': 0.01
    }
    
    exp_id, results = workflow.run_ml_experiment(model, dataset, params)
    experiment_results.append((model, results))
    
    print(f"Model: {model}")
    print(f"Accuracy: {results['accuracy']:.3f}")
    print(f"Training Time: {results['training_time_seconds']}s")
    print(f"Inference Time: {results['inference_time_ms']:.1f}ms")
    print("---")

# Analyze results
results_df = pd.DataFrame([
    {'model': model, **results} 
    for model, results in experiment_results
])

print("\nExperiment Summary:")
print(results_df[['model', 'accuracy', 'training_time_seconds', 'inference_time_ms']].to_string(index=False))

# Find best model
best_model = results_df.loc[results_df['accuracy'].idxmax()]
print(f"\nBest performing model: {best_model['model']} with accuracy: {best_model['accuracy']:.3f}")
```

## Usage Examples

### Expected Output

```text
Starting ML experiment: RandomForest on sentiment_analysis_v3
Created experiment: 123
Added 5 documents to randomforest_sentiment_analysis_v3
Experiment 123 completed successfully!
Model: RandomForest
Accuracy: 0.862
Training Time: 128s
Inference Time: 14.2ms
---

Experiment Summary:
       model  accuracy  training_time_seconds  inference_time_ms
0  NeuralNet     0.913                    445               24.8
1    XGBoost     0.881                    172               11.9
2 RandomForest     0.862                    128               14.2

Best performing model: NeuralNet with accuracy: 0.913
```

## Key Architecture Benefits

1. **PostgreSQL + pgvector**:
   - Structured experiment metadata with ACID properties
   - Vector similarity search with SQL integration
   - Strong consistency for experiment tracking

2. **Chroma**:
   - Optimized for high-dimensional vector operations
   - Fast similarity search and retrieval
   - Simple REST API for document management

3. **DuckDB**:
   - Columnar storage for analytical queries
   - Excellent performance for aggregations
   - In-process analytics without external dependencies

4. **Integration Pattern**:
   - Each database serves a specific purpose
   - Clean separation of concerns
   - Scalable and maintainable architecture

## Notes

- **Prerequisites**: PostgreSQL with pgvector extension, Chroma server, DuckDB installation
- **Performance**: Vector operations are CPU-intensive; consider using appropriate indexing
- **Security**: Use environment variables for database credentials in production
- **Scalability**: Consider connection pooling and async operations for high-throughput scenarios
- **Monitoring**: Add logging and metrics collection for production deployments

## Related Snippets

- [PostgreSQL Connection Patterns](../sql/postgresql-connection-patterns.md)
- [Vector Database Operations](../python/vector-database-operations.md)
- [Data Analytics with DuckDB](../python/duckdb-analytics.md)
- [ML Experiment Tracking](../python/ml-experiment-tracking.md)