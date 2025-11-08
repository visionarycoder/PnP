namespace Python.DataProcessing;

/// <summary>
/// Demonstrates Python data processing patterns using pandas, NumPy, and other libraries
/// </summary>
public static class Program
{
    public static void Main()
    {
        Console.WriteLine("Python Data Processing Patterns");
        Console.WriteLine("==============================");
        
        PandasBasics();
        DataCleaning();
        DataTransformation();
        DataAggregation();
        TimeSeriesAnalysis();
        DataVisualization();
        PerformanceOptimization();
    }

    /// <summary>
    /// Basic pandas operations and DataFrame manipulation
    /// </summary>
    private static void PandasBasics()
    {
        Console.WriteLine("\n1. Pandas Basics:");
        
        Console.WriteLine("# Import libraries");
        Console.WriteLine("import pandas as pd");
        Console.WriteLine("import numpy as np");
        Console.WriteLine("from datetime import datetime, timedelta");
        
        Console.WriteLine("\n# Creating DataFrames");
        Console.WriteLine("# From dictionary");
        Console.WriteLine("data = {");
        Console.WriteLine("    'name': ['Alice', 'Bob', 'Charlie', 'Diana'],");
        Console.WriteLine("    'age': [25, 30, 35, 28],");
        Console.WriteLine("    'salary': [50000, 60000, 70000, 55000],");
        Console.WriteLine("    'department': ['IT', 'HR', 'Finance', 'IT']");
        Console.WriteLine("}");
        Console.WriteLine("df = pd.DataFrame(data)");
        
        Console.WriteLine("\n# From CSV file");
        Console.WriteLine("df = pd.read_csv('employees.csv')");
        Console.WriteLine("df = pd.read_csv('data.csv', encoding='utf-8', sep=';')");
        
        Console.WriteLine("\n# Basic DataFrame operations");
        Console.WriteLine("print(df.head())         # First 5 rows");
        Console.WriteLine("print(df.tail(3))        # Last 3 rows");
        Console.WriteLine("print(df.shape)          # Dimensions");
        Console.WriteLine("print(df.info())         # Data types and non-null counts");
        Console.WriteLine("print(df.describe())     # Statistical summary");
        Console.WriteLine("print(df.columns)        # Column names");
        Console.WriteLine("print(df.dtypes)         # Data types");
        
        Console.WriteLine("\n# Selecting data");
        Console.WriteLine("# Single column");
        Console.WriteLine("names = df['name']");
        Console.WriteLine("# Multiple columns");
        Console.WriteLine("subset = df[['name', 'salary']]");
        Console.WriteLine("# Row selection");
        Console.WriteLine("first_row = df.iloc[0]");
        Console.WriteLine("first_three = df.iloc[0:3]");
        Console.WriteLine("by_label = df.loc[df['age'] > 30]");
        
        Console.WriteLine("\n# Filtering");
        Console.WriteLine("it_employees = df[df['department'] == 'IT']");
        Console.WriteLine("high_salary = df[df['salary'] > 55000]");
        Console.WriteLine("complex_filter = df[(df['age'] > 25) & (df['salary'] < 65000)]");
        Console.WriteLine("name_filter = df[df['name'].isin(['Alice', 'Bob'])]");
    }

    /// <summary>
    /// Data cleaning and preprocessing techniques
    /// </summary>
    private static void DataCleaning()
    {
        Console.WriteLine("\n2. Data Cleaning:");
        
        Console.WriteLine("# Handling missing values");
        Console.WriteLine("# Check for missing values");
        Console.WriteLine("print(df.isnull().sum())");
        Console.WriteLine("print(df.isna().any())");
        
        Console.WriteLine("\n# Remove missing values");
        Console.WriteLine("df_clean = df.dropna()                    # Drop rows with any NaN");
        Console.WriteLine("df_clean = df.dropna(subset=['salary'])   # Drop rows with NaN in specific column");
        Console.WriteLine("df_clean = df.dropna(axis=1)             # Drop columns with any NaN");
        
        Console.WriteLine("\n# Fill missing values");
        Console.WriteLine("df['salary'].fillna(df['salary'].mean(), inplace=True)");
        Console.WriteLine("df['department'].fillna('Unknown', inplace=True)");
        Console.WriteLine("df.fillna(method='ffill', inplace=True)   # Forward fill");
        Console.WriteLine("df.fillna(method='bfill', inplace=True)   # Backward fill");
        
        Console.WriteLine("\n# Interpolation for numeric data");
        Console.WriteLine("df['salary'].interpolate(method='linear', inplace=True)");
        Console.WriteLine("df['salary'].interpolate(method='polynomial', order=2, inplace=True)");
        
        Console.WriteLine("\n# Removing duplicates");
        Console.WriteLine("print(df.duplicated().sum())");
        Console.WriteLine("df_unique = df.drop_duplicates()");
        Console.WriteLine("df_unique = df.drop_duplicates(subset=['name'], keep='first')");
        
        Console.WriteLine("\n# Data type conversions");
        Console.WriteLine("df['age'] = df['age'].astype(int)");
        Console.WriteLine("df['salary'] = pd.to_numeric(df['salary'], errors='coerce')");
        Console.WriteLine("df['hire_date'] = pd.to_datetime(df['hire_date'])");
        Console.WriteLine("df['department'] = df['department'].astype('category')");
        
        Console.WriteLine("\n# String cleaning");
        Console.WriteLine("# Remove whitespace");
        Console.WriteLine("df['name'] = df['name'].str.strip()");
        Console.WriteLine("# Convert to lowercase");
        Console.WriteLine("df['department'] = df['department'].str.lower()");
        Console.WriteLine("# Replace values");
        Console.WriteLine("df['department'] = df['department'].str.replace('information technology', 'it')");
        Console.WriteLine("# Remove special characters");
        Console.WriteLine("df['name'] = df['name'].str.replace('[^a-zA-Z\\s]', '', regex=True)");
        
        Console.WriteLine("\n# Outlier detection and handling");
        Console.WriteLine("# Using IQR method");
        Console.WriteLine("Q1 = df['salary'].quantile(0.25)");
        Console.WriteLine("Q3 = df['salary'].quantile(0.75)");
        Console.WriteLine("IQR = Q3 - Q1");
        Console.WriteLine("lower_bound = Q1 - 1.5 * IQR");
        Console.WriteLine("upper_bound = Q3 + 1.5 * IQR");
        Console.WriteLine("outliers = df[(df['salary'] < lower_bound) | (df['salary'] > upper_bound)]");
        Console.WriteLine("df_no_outliers = df[(df['salary'] >= lower_bound) & (df['salary'] <= upper_bound)]");
        
        Console.WriteLine("\n# Using Z-score method");
        Console.WriteLine("from scipy import stats");
        Console.WriteLine("z_scores = np.abs(stats.zscore(df['salary']))");
        Console.WriteLine("threshold = 3");
        Console.WriteLine("df_no_outliers = df[z_scores < threshold]");
    }

    /// <summary>
    /// Data transformation and feature engineering
    /// </summary>
    private static void DataTransformation()
    {
        Console.WriteLine("\n3. Data Transformation:");
        
        Console.WriteLine("# Creating new columns");
        Console.WriteLine("df['salary_category'] = df['salary'].apply(");
        Console.WriteLine("    lambda x: 'High' if x > 60000 else 'Medium' if x > 50000 else 'Low'");
        Console.WriteLine(")");
        Console.WriteLine("df['years_experience'] = 2024 - df['hire_year']");
        Console.WriteLine("df['full_name'] = df['first_name'] + ' ' + df['last_name']");
        
        Console.WriteLine("\n# Mathematical transformations");
        Console.WriteLine("df['log_salary'] = np.log(df['salary'])");
        Console.WriteLine("df['sqrt_age'] = np.sqrt(df['age'])");
        Console.WriteLine("df['salary_squared'] = df['salary'] ** 2");
        
        Console.WriteLine("\n# Binning continuous variables");
        Console.WriteLine("age_bins = [0, 25, 35, 50, 100]");
        Console.WriteLine("age_labels = ['Young', 'Adult', 'Middle-aged', 'Senior']");
        Console.WriteLine("df['age_group'] = pd.cut(df['age'], bins=age_bins, labels=age_labels)");
        
        Console.WriteLine("\n# Equal-frequency binning");
        Console.WriteLine("df['salary_quartile'] = pd.qcut(df['salary'], q=4, labels=['Q1', 'Q2', 'Q3', 'Q4'])");
        
        Console.WriteLine("\n# One-hot encoding");
        Console.WriteLine("df_encoded = pd.get_dummies(df, columns=['department'], prefix='dept')");
        Console.WriteLine("# Alternative using sklearn");
        Console.WriteLine("from sklearn.preprocessing import OneHotEncoder");
        Console.WriteLine("encoder = OneHotEncoder(sparse=False)");
        Console.WriteLine("dept_encoded = encoder.fit_transform(df[['department']])");
        
        Console.WriteLine("\n# Label encoding");
        Console.WriteLine("from sklearn.preprocessing import LabelEncoder");
        Console.WriteLine("le = LabelEncoder()");
        Console.WriteLine("df['department_encoded'] = le.fit_transform(df['department'])");
        
        Console.WriteLine("\n# Scaling and normalization");
        Console.WriteLine("from sklearn.preprocessing import StandardScaler, MinMaxScaler");
        Console.WriteLine("# Standard scaling (z-score normalization)");
        Console.WriteLine("scaler = StandardScaler()");
        Console.WriteLine("df['salary_scaled'] = scaler.fit_transform(df[['salary']])");
        Console.WriteLine("# Min-max scaling");
        Console.WriteLine("min_max_scaler = MinMaxScaler()");
        Console.WriteLine("df['age_normalized'] = min_max_scaler.fit_transform(df[['age']])");
        
        Console.WriteLine("\n# Date feature engineering");
        Console.WriteLine("df['hire_date'] = pd.to_datetime(df['hire_date'])");
        Console.WriteLine("df['hire_year'] = df['hire_date'].dt.year");
        Console.WriteLine("df['hire_month'] = df['hire_date'].dt.month");
        Console.WriteLine("df['hire_weekday'] = df['hire_date'].dt.dayofweek");
        Console.WriteLine("df['hire_quarter'] = df['hire_date'].dt.quarter");
        Console.WriteLine("df['days_since_hire'] = (datetime.now() - df['hire_date']).dt.days");
    }

    /// <summary>
    /// Data aggregation and grouping operations
    /// </summary>
    private static void DataAggregation()
    {
        Console.WriteLine("\n4. Data Aggregation:");
        
        Console.WriteLine("# Basic groupby operations");
        Console.WriteLine("# Group by single column");
        Console.WriteLine("dept_stats = df.groupby('department')['salary'].agg(['mean', 'median', 'std', 'count'])");
        Console.WriteLine("print(dept_stats)");
        
        Console.WriteLine("\n# Group by multiple columns");
        Console.WriteLine("complex_group = df.groupby(['department', 'age_group'])['salary'].mean()");
        Console.WriteLine("print(complex_group.unstack())  # Pivot to show age_group as columns");
        
        Console.WriteLine("\n# Multiple aggregations");
        Console.WriteLine("agg_dict = {");
        Console.WriteLine("    'salary': ['mean', 'max', 'min'],");
        Console.WriteLine("    'age': ['mean', 'std'],");
        Console.WriteLine("    'name': 'count'");
        Console.WriteLine("}");
        Console.WriteLine("result = df.groupby('department').agg(agg_dict)");
        
        Console.WriteLine("\n# Custom aggregation functions");
        Console.WriteLine("def salary_range(x):");
        Console.WriteLine("    return x.max() - x.min()");
        Console.WriteLine("def top_performer(x):");
        Console.WriteLine("    return x.nlargest(1).iloc[0] if len(x) > 0 else None");
        Console.WriteLine("custom_agg = df.groupby('department')['salary'].agg([salary_range, top_performer])");
        
        Console.WriteLine("\n# Rolling aggregations");
        Console.WriteLine("df_sorted = df.sort_values('hire_date')");
        Console.WriteLine("df_sorted['salary_rolling_mean'] = df_sorted['salary'].rolling(window=3).mean()");
        Console.WriteLine("df_sorted['salary_expanding_mean'] = df_sorted['salary'].expanding().mean()");
        
        Console.WriteLine("\n# Pivot tables");
        Console.WriteLine("pivot = pd.pivot_table(");
        Console.WriteLine("    df,");
        Console.WriteLine("    values='salary',");
        Console.WriteLine("    index='department',");
        Console.WriteLine("    columns='age_group',");
        Console.WriteLine("    aggfunc='mean',");
        Console.WriteLine("    fill_value=0");
        Console.WriteLine(")");
        
        Console.WriteLine("\n# Cross-tabulation");
        Console.WriteLine("crosstab = pd.crosstab(");
        Console.WriteLine("    df['department'],");
        Console.WriteLine("    df['age_group'],");
        Console.WriteLine("    margins=True,");
        Console.WriteLine("    normalize='index'  # Show percentages by row");
        Console.WriteLine(")");
        
        Console.WriteLine("\n# Percentile calculations");
        Console.WriteLine("percentiles = df.groupby('department')['salary'].quantile([0.25, 0.5, 0.75, 0.9])");
        Console.WriteLine("print(percentiles.unstack())");
    }

    /// <summary>
    /// Time series data analysis
    /// </summary>
    private static void TimeSeriesAnalysis()
    {
        Console.WriteLine("\n5. Time Series Analysis:");
        
        Console.WriteLine("# Creating time series data");
        Console.WriteLine("dates = pd.date_range('2020-01-01', periods=1000, freq='D')");
        Console.WriteLine("ts_data = pd.DataFrame({");
        Console.WriteLine("    'date': dates,");
        Console.WriteLine("    'sales': np.random.normal(1000, 100, 1000) + np.sin(np.arange(1000) * 2 * np.pi / 365) * 200");
        Console.WriteLine("})");
        Console.WriteLine("ts_data.set_index('date', inplace=True)");
        
        Console.WriteLine("\n# Resampling");
        Console.WriteLine("# Downsample to monthly averages");
        Console.WriteLine("monthly_avg = ts_data.resample('M').mean()");
        Console.WriteLine("# Downsample to weekly sums");
        Console.WriteLine("weekly_sum = ts_data.resample('W').sum()");
        Console.WriteLine("# Upsample to hourly (with forward fill)");
        Console.WriteLine("hourly_data = ts_data.resample('H').ffill()");
        
        Console.WriteLine("\n# Moving averages and trends");
        Console.WriteLine("ts_data['ma_7'] = ts_data['sales'].rolling(window=7).mean()");
        Console.WriteLine("ts_data['ma_30'] = ts_data['sales'].rolling(window=30).mean()");
        Console.WriteLine("ts_data['ewm_alpha'] = ts_data['sales'].ewm(alpha=0.1).mean()");
        
        Console.WriteLine("\n# Seasonal decomposition");
        Console.WriteLine("from statsmodels.tsa.seasonal import seasonal_decompose");
        Console.WriteLine("decomposition = seasonal_decompose(ts_data['sales'], model='additive', period=365)");
        Console.WriteLine("trend = decomposition.trend");
        Console.WriteLine("seasonal = decomposition.seasonal");
        Console.WriteLine("residual = decomposition.resid");
        
        Console.WriteLine("\n# Date/time operations");
        Console.WriteLine("# Filter by date range");
        Console.WriteLine("recent_data = ts_data['2023-01-01':'2023-12-31']");
        Console.WriteLine("# Group by time components");
        Console.WriteLine("monthly_patterns = ts_data.groupby(ts_data.index.month)['sales'].mean()");
        Console.WriteLine("weekday_patterns = ts_data.groupby(ts_data.index.dayofweek)['sales'].mean()");
        
        Console.WriteLine("\n# Time zone handling");
        Console.WriteLine("# Localize to timezone");
        Console.WriteLine("ts_data.index = ts_data.index.tz_localize('UTC')");
        Console.WriteLine("# Convert timezone");
        Console.WriteLine("ts_data_est = ts_data.tz_convert('US/Eastern')");
        
        Console.WriteLine("\n# Lag features");
        Console.WriteLine("ts_data['sales_lag_1'] = ts_data['sales'].shift(1)");
        Console.WriteLine("ts_data['sales_lag_7'] = ts_data['sales'].shift(7)");
        Console.WriteLine("ts_data['sales_diff'] = ts_data['sales'].diff()");
        Console.WriteLine("ts_data['sales_pct_change'] = ts_data['sales'].pct_change()");
    }

    /// <summary>
    /// Data visualization patterns
    /// </summary>
    private static void DataVisualization()
    {
        Console.WriteLine("\n6. Data Visualization:");
        
        Console.WriteLine("import matplotlib.pyplot as plt");
        Console.WriteLine("import seaborn as sns");
        Console.WriteLine("import plotly.express as px");
        Console.WriteLine("import plotly.graph_objects as go");
        
        Console.WriteLine("\n# Matplotlib basics");
        Console.WriteLine("plt.figure(figsize=(10, 6))");
        Console.WriteLine("plt.plot(df['age'], df['salary'], 'o')");
        Console.WriteLine("plt.xlabel('Age')");
        Console.WriteLine("plt.ylabel('Salary')");
        Console.WriteLine("plt.title('Age vs Salary')");
        Console.WriteLine("plt.grid(True, alpha=0.3)");
        Console.WriteLine("plt.show()");
        
        Console.WriteLine("\n# Seaborn statistical plots");
        Console.WriteLine("# Correlation heatmap");
        Console.WriteLine("plt.figure(figsize=(8, 6))");
        Console.WriteLine("correlation_matrix = df.select_dtypes(include=[np.number]).corr()");
        Console.WriteLine("sns.heatmap(correlation_matrix, annot=True, cmap='coolwarm', center=0)");
        Console.WriteLine("plt.title('Correlation Matrix')");
        Console.WriteLine("plt.show()");
        
        Console.WriteLine("\n# Distribution plots");
        Console.WriteLine("fig, axes = plt.subplots(2, 2, figsize=(12, 10))");
        Console.WriteLine("sns.histplot(df['salary'], kde=True, ax=axes[0,0])");
        Console.WriteLine("sns.boxplot(x='department', y='salary', data=df, ax=axes[0,1])");
        Console.WriteLine("sns.violinplot(x='department', y='salary', data=df, ax=axes[1,0])");
        Console.WriteLine("sns.scatterplot(x='age', y='salary', hue='department', data=df, ax=axes[1,1])");
        Console.WriteLine("plt.tight_layout()");
        Console.WriteLine("plt.show()");
        
        Console.WriteLine("\n# Plotly interactive plots");
        Console.WriteLine("# Interactive scatter plot");
        Console.WriteLine("fig = px.scatter(");
        Console.WriteLine("    df,");
        Console.WriteLine("    x='age',");
        Console.WriteLine("    y='salary',");
        Console.WriteLine("    color='department',");
        Console.WriteLine("    size='years_experience',");
        Console.WriteLine("    hover_data=['name'],");
        Console.WriteLine("    title='Interactive Employee Data'");
        Console.WriteLine(")");
        Console.WriteLine("fig.show()");
        
        Console.WriteLine("\n# Time series plot");
        Console.WriteLine("fig = go.Figure()");
        Console.WriteLine("fig.add_trace(go.Scatter(");
        Console.WriteLine("    x=ts_data.index,");
        Console.WriteLine("    y=ts_data['sales'],");
        Console.WriteLine("    mode='lines',");
        Console.WriteLine("    name='Sales',");
        Console.WriteLine("    line=dict(color='blue')");
        Console.WriteLine("))");
        Console.WriteLine("fig.add_trace(go.Scatter(");
        Console.WriteLine("    x=ts_data.index,");
        Console.WriteLine("    y=ts_data['ma_30'],");
        Console.WriteLine("    mode='lines',");
        Console.WriteLine("    name='30-day MA',");
        Console.WriteLine("    line=dict(color='red', dash='dash')");
        Console.WriteLine("))");
        Console.WriteLine("fig.update_layout(title='Sales Trend with Moving Average')");
        Console.WriteLine("fig.show()");
    }

    /// <summary>
    /// Performance optimization techniques
    /// </summary>
    private static void PerformanceOptimization()
    {
        Console.WriteLine("\n7. Performance Optimization:");
        
        Console.WriteLine("# Memory optimization");
        Console.WriteLine("# Check memory usage");
        Console.WriteLine("print(df.info(memory_usage='deep'))");
        Console.WriteLine("print(df.memory_usage(deep=True).sum())");
        
        Console.WriteLine("\n# Optimize data types");
        Console.WriteLine("# Downcast numeric types");
        Console.WriteLine("df['age'] = pd.to_numeric(df['age'], downcast='integer')");
        Console.WriteLine("df['salary'] = pd.to_numeric(df['salary'], downcast='float')");
        Console.WriteLine("# Use categorical for strings");
        Console.WriteLine("df['department'] = df['department'].astype('category')");
        
        Console.WriteLine("\n# Efficient file I/O");
        Console.WriteLine("# Use efficient formats");
        Console.WriteLine("df.to_parquet('data.parquet')  # More efficient than CSV");
        Console.WriteLine("df.to_pickle('data.pkl')       # Fastest for pandas objects");
        Console.WriteLine("df.to_hdf('data.h5', key='df') # Good for large datasets");
        
        Console.WriteLine("\n# Chunked processing for large files");
        Console.WriteLine("chunk_size = 10000");
        Console.WriteLine("chunks = []");
        Console.WriteLine("for chunk in pd.read_csv('large_file.csv', chunksize=chunk_size):");
        Console.WriteLine("    # Process each chunk");
        Console.WriteLine("    processed_chunk = chunk.groupby('category').sum()");
        Console.WriteLine("    chunks.append(processed_chunk)");
        Console.WriteLine("result = pd.concat(chunks, ignore_index=True)");
        
        Console.WriteLine("\n# Vectorized operations vs loops");
        Console.WriteLine("# Avoid loops - use vectorized operations");
        Console.WriteLine("# Slow - using apply with lambda");
        Console.WriteLine("# df['salary_category'] = df['salary'].apply(lambda x: 'High' if x > 60000 else 'Low')");
        Console.WriteLine("# Fast - using numpy.where");
        Console.WriteLine("df['salary_category'] = np.where(df['salary'] > 60000, 'High', 'Low')");
        
        Console.WriteLine("\n# Use query for complex filtering");
        Console.WriteLine("# More efficient than boolean indexing for complex queries");
        Console.WriteLine("result = df.query('salary > 50000 and age < 35 and department == \"IT\"')");
        
        Console.WriteLine("\n# Parallel processing with multiprocessing");
        Console.WriteLine("import multiprocessing as mp");
        Console.WriteLine("from functools import partial");
        Console.WriteLine("def process_group(group_data):");
        Console.WriteLine("    # Some expensive computation");
        Console.WriteLine("    return group_data.groupby('category').sum()");
        Console.WriteLine("# Split data and process in parallel");
        Console.WriteLine("with mp.Pool(processes=mp.cpu_count()) as pool:");
        Console.WriteLine("    results = pool.map(process_group, data_chunks)");
        
        Console.WriteLine("\n# Using Dask for larger-than-memory datasets");
        Console.WriteLine("import dask.dataframe as dd");
        Console.WriteLine("# Read large CSV with Dask");
        Console.WriteLine("ddf = dd.read_csv('very_large_file.csv')");
        Console.WriteLine("# Perform operations (lazy evaluation)");
        Console.WriteLine("result = ddf.groupby('category').salary.mean()");
        Console.WriteLine("# Compute result");
        Console.WriteLine("final_result = result.compute()");
        
        Console.WriteLine("\n# Index optimization");
        Console.WriteLine("# Set appropriate index for faster lookups");
        Console.WriteLine("df_indexed = df.set_index('employee_id')");
        Console.WriteLine("# Use .loc for fast label-based selection");
        Console.WriteLine("employee = df_indexed.loc[12345]");
        Console.WriteLine("# Create multi-level index for hierarchical data");
        Console.WriteLine("df_multi = df.set_index(['department', 'employee_id'])");
    }
}