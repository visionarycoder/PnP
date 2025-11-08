namespace Python.APIIntegration;

/// <summary>
/// Demonstrates Python API integration patterns using requests, authentication, and async operations
/// </summary>
public static class Program
{
    public static void Main()
    {
        Console.WriteLine("Python API Integration Patterns");
        Console.WriteLine("==============================");
        
        BasicRequestsUsage();
        AuthenticationPatterns();
        ErrorHandlingAndRetry();
        AsyncRequestPatterns();
        RESTClientPatterns();
        GraphQLIntegration();
        WebhookHandling();
        APITestingPatterns();
    }

    /// <summary>
    /// Basic HTTP requests with requests library
    /// </summary>
    private static void BasicRequestsUsage()
    {
        Console.WriteLine("\n1. Basic Requests Usage:");
        
        Console.WriteLine("import requests");
        Console.WriteLine("import json");
        Console.WriteLine("from urllib.parse import urljoin, urlencode");
        
        Console.WriteLine("\n# GET request");
        Console.WriteLine("response = requests.get('https://api.example.com/users')");
        Console.WriteLine("print(f'Status Code: {response.status_code}')");
        Console.WriteLine("print(f'Headers: {response.headers}')");
        Console.WriteLine("data = response.json()");
        
        Console.WriteLine("\n# GET with parameters");
        Console.WriteLine("params = {'page': 1, 'limit': 10, 'sort': 'name'}");
        Console.WriteLine("response = requests.get('https://api.example.com/users', params=params)");
        Console.WriteLine("# URL becomes: https://api.example.com/users?page=1&limit=10&sort=name");
        
        Console.WriteLine("\n# POST request with JSON data");
        Console.WriteLine("new_user = {");
        Console.WriteLine("    'name': 'John Doe',");
        Console.WriteLine("    'email': 'john@example.com',");
        Console.WriteLine("    'age': 30");
        Console.WriteLine("}");
        Console.WriteLine("response = requests.post(");
        Console.WriteLine("    'https://api.example.com/users',");
        Console.WriteLine("    json=new_user,");
        Console.WriteLine("    headers={'Content-Type': 'application/json'}");
        Console.WriteLine(")");
        
        Console.WriteLine("\n# POST with form data");
        Console.WriteLine("form_data = {'username': 'johndoe', 'password': 'secret'}");
        Console.WriteLine("response = requests.post('https://api.example.com/login', data=form_data)");
        
        Console.WriteLine("\n# PUT and PATCH requests");
        Console.WriteLine("updated_user = {'name': 'John Smith', 'age': 31}");
        Console.WriteLine("response = requests.put('https://api.example.com/users/123', json=updated_user)");
        Console.WriteLine("response = requests.patch('https://api.example.com/users/123', json={'age': 32})");
        
        Console.WriteLine("\n# DELETE request");
        Console.WriteLine("response = requests.delete('https://api.example.com/users/123')");
        
        Console.WriteLine("\n# File upload");
        Console.WriteLine("files = {'file': open('document.pdf', 'rb')}");
        Console.WriteLine("response = requests.post('https://api.example.com/upload', files=files)");
        Console.WriteLine("files['file'].close()");
        
        Console.WriteLine("\n# Custom headers and timeouts");
        Console.WriteLine("headers = {");
        Console.WriteLine("    'User-Agent': 'MyApp/1.0',");
        Console.WriteLine("    'Accept': 'application/json',");
        Console.WriteLine("    'Authorization': 'Bearer YOUR_TOKEN'");
        Console.WriteLine("}");
        Console.WriteLine("response = requests.get(");
        Console.WriteLine("    'https://api.example.com/data',");
        Console.WriteLine("    headers=headers,");
        Console.WriteLine("    timeout=(5, 30)  # (connect_timeout, read_timeout)");
        Console.WriteLine(")");
    }

    /// <summary>
    /// Authentication patterns for APIs
    /// </summary>
    private static void AuthenticationPatterns()
    {
        Console.WriteLine("\n2. Authentication Patterns:");
        
        Console.WriteLine("# API Key authentication");
        Console.WriteLine("api_key = 'your_api_key_here'");
        Console.WriteLine("# In header");
        Console.WriteLine("headers = {'X-API-Key': api_key}");
        Console.WriteLine("response = requests.get('https://api.example.com/data', headers=headers)");
        Console.WriteLine("# In query parameter");
        Console.WriteLine("response = requests.get('https://api.example.com/data?api_key=' + api_key)");
        
        Console.WriteLine("\n# Basic Authentication");
        Console.WriteLine("from requests.auth import HTTPBasicAuth");
        Console.WriteLine("response = requests.get(");
        Console.WriteLine("    'https://api.example.com/data',");
        Console.WriteLine("    auth=HTTPBasicAuth('username', 'password')");
        Console.WriteLine(")");
        Console.WriteLine("# Alternative syntax");
        Console.WriteLine("response = requests.get('https://api.example.com/data', auth=('username', 'password'))");
        
        Console.WriteLine("\n# Bearer Token authentication");
        Console.WriteLine("token = 'your_bearer_token'");
        Console.WriteLine("headers = {'Authorization': f'Bearer {token}'}");
        Console.WriteLine("response = requests.get('https://api.example.com/data', headers=headers)");
        
        Console.WriteLine("\n# OAuth 2.0 flow");
        Console.WriteLine("from requests_oauthlib import OAuth2Session");
        Console.WriteLine("# Authorization code flow");
        Console.WriteLine("client_id = 'your_client_id'");
        Console.WriteLine("redirect_uri = 'http://localhost:8080/callback'");
        Console.WriteLine("authorization_base_url = 'https://provider.com/oauth/authorize'");
        Console.WriteLine("token_url = 'https://provider.com/oauth/token'");
        Console.WriteLine("oauth = OAuth2Session(client_id, redirect_uri=redirect_uri)");
        Console.WriteLine("authorization_url, state = oauth.authorization_url(authorization_base_url)");
        Console.WriteLine("print(f'Please go to {authorization_url} and authorize access.')");
        
        Console.WriteLine("\n# JWT token handling");
        Console.WriteLine("import jwt");
        Console.WriteLine("from datetime import datetime, timedelta");
        Console.WriteLine("def create_jwt_token(payload, secret, algorithm='HS256'):");
        Console.WriteLine("    payload['exp'] = datetime.utcnow() + timedelta(hours=1)");
        Console.WriteLine("    return jwt.encode(payload, secret, algorithm=algorithm)");
        Console.WriteLine("def decode_jwt_token(token, secret, algorithm='HS256'):");
        Console.WriteLine("    try:");
        Console.WriteLine("        return jwt.decode(token, secret, algorithms=[algorithm])");
        Console.WriteLine("    except jwt.ExpiredSignatureError:");
        Console.WriteLine("        return None");
        
        Console.WriteLine("\n# Session management");
        Console.WriteLine("session = requests.Session()");
        Console.WriteLine("session.headers.update({'User-Agent': 'MyApp/1.0'})");
        Console.WriteLine("# Login and store cookies");
        Console.WriteLine("login_data = {'username': 'user', 'password': 'pass'}");
        Console.WriteLine("session.post('https://api.example.com/login', data=login_data)");
        Console.WriteLine("# Subsequent requests use stored cookies");
        Console.WriteLine("response = session.get('https://api.example.com/protected')");
        
        Console.WriteLine("\n# Token refresh pattern");
        Console.WriteLine("class TokenManager:");
        Console.WriteLine("    def __init__(self, client_id, client_secret, token_url):");
        Console.WriteLine("        self.client_id = client_id");
        Console.WriteLine("        self.client_secret = client_secret");
        Console.WriteLine("        self.token_url = token_url");
        Console.WriteLine("        self.access_token = None");
        Console.WriteLine("        self.refresh_token = None");
        Console.WriteLine("        self.token_expiry = None");
        Console.WriteLine("    ");
        Console.WriteLine("    def get_valid_token(self):");
        Console.WriteLine("        if self.is_token_expired():");
        Console.WriteLine("            self.refresh_access_token()");
        Console.WriteLine("        return self.access_token");
        Console.WriteLine("    ");
        Console.WriteLine("    def is_token_expired(self):");
        Console.WriteLine("        return (self.token_expiry is None or ");
        Console.WriteLine("                datetime.utcnow() >= self.token_expiry)");
    }

    /// <summary>
    /// Error handling and retry mechanisms
    /// </summary>
    private static void ErrorHandlingAndRetry()
    {
        Console.WriteLine("\n3. Error Handling and Retry:");
        
        Console.WriteLine("import time");
        Console.WriteLine("from requests.exceptions import RequestException, Timeout, ConnectionError");
        Console.WriteLine("from urllib3.util.retry import Retry");
        Console.WriteLine("from requests.adapters import HTTPAdapter");
        
        Console.WriteLine("\n# Basic error handling");
        Console.WriteLine("try:");
        Console.WriteLine("    response = requests.get('https://api.example.com/data', timeout=10)");
        Console.WriteLine("    response.raise_for_status()  # Raises HTTPError for bad responses");
        Console.WriteLine("    data = response.json()");
        Console.WriteLine("except requests.exceptions.Timeout:");
        Console.WriteLine("    print('Request timed out')");
        Console.WriteLine("except requests.exceptions.ConnectionError:");
        Console.WriteLine("    print('Connection error occurred')");
        Console.WriteLine("except requests.exceptions.HTTPError as e:");
        Console.WriteLine("    print(f'HTTP error occurred: {e.response.status_code}')");
        Console.WriteLine("except requests.exceptions.RequestException as e:");
        Console.WriteLine("    print(f'Request error: {e}')");
        Console.WriteLine("except ValueError:");
        Console.WriteLine("    print('Invalid JSON response')");
        
        Console.WriteLine("\n# Custom retry strategy");
        Console.WriteLine("def make_request_with_retry(url, max_retries=3, backoff_factor=1):");
        Console.WriteLine("    for attempt in range(max_retries + 1):");
        Console.WriteLine("        try:");
        Console.WriteLine("            response = requests.get(url, timeout=10)");
        Console.WriteLine("            response.raise_for_status()");
        Console.WriteLine("            return response");
        Console.WriteLine("        except (ConnectionError, Timeout) as e:");
        Console.WriteLine("            if attempt == max_retries:");
        Console.WriteLine("                raise e");
        Console.WriteLine("            wait_time = backoff_factor * (2 ** attempt)");
        Console.WriteLine("            print(f'Attempt {attempt + 1} failed, retrying in {wait_time}s...')");
        Console.WriteLine("            time.sleep(wait_time)");
        
        Console.WriteLine("\n# urllib3 retry strategy");
        Console.WriteLine("retry_strategy = Retry(");
        Console.WriteLine("    total=3,");
        Console.WriteLine("    status_forcelist=[429, 500, 502, 503, 504],");
        Console.WriteLine("    method_whitelist=['HEAD', 'GET', 'OPTIONS'],");
        Console.WriteLine("    backoff_factor=1");
        Console.WriteLine(")");
        Console.WriteLine("adapter = HTTPAdapter(max_retries=retry_strategy)");
        Console.WriteLine("session = requests.Session()");
        Console.WriteLine("session.mount('http://', adapter)");
        Console.WriteLine("session.mount('https://', adapter)");
        
        Console.WriteLine("\n# Exponential backoff with jitter");
        Console.WriteLine("import random");
        Console.WriteLine("def exponential_backoff_with_jitter(attempt, base_delay=1, max_delay=60):");
        Console.WriteLine("    delay = min(base_delay * (2 ** attempt), max_delay)");
        Console.WriteLine("    jitter = delay * random.uniform(0.1, 1.0)");
        Console.WriteLine("    return jitter");
        
        Console.WriteLine("\n# Rate limiting handling");
        Console.WriteLine("def handle_rate_limit(response):");
        Console.WriteLine("    if response.status_code == 429:");
        Console.WriteLine("        retry_after = response.headers.get('Retry-After')");
        Console.WriteLine("        if retry_after:");
        Console.WriteLine("            sleep_time = int(retry_after)");
        Console.WriteLine("            print(f'Rate limited. Waiting {sleep_time} seconds...')");
        Console.WriteLine("            time.sleep(sleep_time)");
        Console.WriteLine("            return True");
        Console.WriteLine("    return False");
        
        Console.WriteLine("\n# Circuit breaker pattern");
        Console.WriteLine("class CircuitBreaker:");
        Console.WriteLine("    def __init__(self, failure_threshold=5, timeout=60):");
        Console.WriteLine("        self.failure_threshold = failure_threshold");
        Console.WriteLine("        self.timeout = timeout");
        Console.WriteLine("        self.failure_count = 0");
        Console.WriteLine("        self.last_failure_time = None");
        Console.WriteLine("        self.state = 'CLOSED'  # CLOSED, OPEN, HALF_OPEN");
        Console.WriteLine("    ");
        Console.WriteLine("    def call(self, func, *args, **kwargs):");
        Console.WriteLine("        if self.state == 'OPEN':");
        Console.WriteLine("            if time.time() - self.last_failure_time > self.timeout:");
        Console.WriteLine("                self.state = 'HALF_OPEN'");
        Console.WriteLine("            else:");
        Console.WriteLine("                raise Exception('Circuit breaker is OPEN')");
        Console.WriteLine("        ");
        Console.WriteLine("        try:");
        Console.WriteLine("            result = func(*args, **kwargs)");
        Console.WriteLine("            self.on_success()");
        Console.WriteLine("            return result");
        Console.WriteLine("        except Exception as e:");
        Console.WriteLine("            self.on_failure()");
        Console.WriteLine("            raise e");
    }

    /// <summary>
    /// Asynchronous request patterns
    /// </summary>
    private static void AsyncRequestPatterns()
    {
        Console.WriteLine("\n4. Async Request Patterns:");
        
        Console.WriteLine("import asyncio");
        Console.WriteLine("import aiohttp");
        Console.WriteLine("from aiohttp import ClientSession, ClientTimeout");
        
        Console.WriteLine("\n# Basic async request");
        Console.WriteLine("async def fetch_data(url):");
        Console.WriteLine("    async with aiohttp.ClientSession() as session:");
        Console.WriteLine("        async with session.get(url) as response:");
        Console.WriteLine("            return await response.json()");
        Console.WriteLine("");
        Console.WriteLine("# Usage");
        Console.WriteLine("async def main():");
        Console.WriteLine("    data = await fetch_data('https://api.example.com/data')");
        Console.WriteLine("    print(data)");
        Console.WriteLine("asyncio.run(main())");
        
        Console.WriteLine("\n# Concurrent requests");
        Console.WriteLine("async def fetch_multiple_urls(urls):");
        Console.WriteLine("    async with aiohttp.ClientSession() as session:");
        Console.WriteLine("        tasks = [fetch_single_url(session, url) for url in urls]");
        Console.WriteLine("        return await asyncio.gather(*tasks)");
        Console.WriteLine("");
        Console.WriteLine("async def fetch_single_url(session, url):");
        Console.WriteLine("    try:");
        Console.WriteLine("        async with session.get(url) as response:");
        Console.WriteLine("            return await response.json()");
        Console.WriteLine("    except Exception as e:");
        Console.WriteLine("        print(f'Error fetching {url}: {e}')");
        Console.WriteLine("        return None");
        
        Console.WriteLine("\n# Async with rate limiting");
        Console.WriteLine("import asyncio");
        Console.WriteLine("from asyncio import Semaphore");
        Console.WriteLine("async def rate_limited_fetch(session, url, semaphore, delay=1):");
        Console.WriteLine("    async with semaphore:");
        Console.WriteLine("        try:");
        Console.WriteLine("            async with session.get(url) as response:");
        Console.WriteLine("                result = await response.json()");
        Console.WriteLine("                await asyncio.sleep(delay)  # Rate limiting");
        Console.WriteLine("                return result");
        Console.WriteLine("        except Exception as e:");
        Console.WriteLine("            print(f'Error: {e}')");
        Console.WriteLine("            return None");
        
        Console.WriteLine("\n# Async session with custom settings");
        Console.WriteLine("async def create_session():");
        Console.WriteLine("    timeout = ClientTimeout(total=30, connect=10)");
        Console.WriteLine("    connector = aiohttp.TCPConnector(");
        Console.WriteLine("        limit=100,  # Total connection pool size");
        Console.WriteLine("        limit_per_host=10,  # Per host connection limit");
        Console.WriteLine("        ttl_dns_cache=300,  # DNS cache TTL");
        Console.WriteLine("        use_dns_cache=True");
        Console.WriteLine("    )");
        Console.WriteLine("    return aiohttp.ClientSession(");
        Console.WriteLine("        timeout=timeout,");
        Console.WriteLine("        connector=connector,");
        Console.WriteLine("        headers={'User-Agent': 'AsyncClient/1.0'}");
        Console.WriteLine("    )");
        
        Console.WriteLine("\n# Streaming responses");
        Console.WriteLine("async def stream_download(url, filename):");
        Console.WriteLine("    async with aiohttp.ClientSession() as session:");
        Console.WriteLine("        async with session.get(url) as response:");
        Console.WriteLine("            with open(filename, 'wb') as file:");
        Console.WriteLine("                async for chunk in response.content.iter_chunked(8192):");
        Console.WriteLine("                    file.write(chunk)");
        
        Console.WriteLine("\n# WebSocket client");
        Console.WriteLine("async def websocket_client():");
        Console.WriteLine("    async with aiohttp.ClientSession() as session:");
        Console.WriteLine("        async with session.ws_connect('ws://localhost:8080/ws') as ws:");
        Console.WriteLine("            await ws.send_str('Hello WebSocket!')");
        Console.WriteLine("            async for msg in ws:");
        Console.WriteLine("                if msg.type == aiohttp.WSMsgType.TEXT:");
        Console.WriteLine("                    print(f'Received: {msg.data}')");
        Console.WriteLine("                elif msg.type == aiohttp.WSMsgType.ERROR:");
        Console.WriteLine("                    print(f'WebSocket error: {ws.exception()}')");
        Console.WriteLine("                    break");
    }

    /// <summary>
    /// REST client patterns and utilities
    /// </summary>
    private static void RESTClientPatterns()
    {
        Console.WriteLine("\n5. REST Client Patterns:");
        
        Console.WriteLine("# REST client class");
        Console.WriteLine("class RESTClient:");
        Console.WriteLine("    def __init__(self, base_url, headers=None, auth=None):");
        Console.WriteLine("        self.base_url = base_url.rstrip('/')");
        Console.WriteLine("        self.session = requests.Session()");
        Console.WriteLine("        if headers:");
        Console.WriteLine("            self.session.headers.update(headers)");
        Console.WriteLine("        if auth:");
        Console.WriteLine("            self.session.auth = auth");
        Console.WriteLine("    ");
        Console.WriteLine("    def _url(self, endpoint):");
        Console.WriteLine("        return f'{self.base_url}/{endpoint.lstrip(\"/\")}'");
        Console.WriteLine("    ");
        Console.WriteLine("    def get(self, endpoint, params=None, **kwargs):");
        Console.WriteLine("        response = self.session.get(self._url(endpoint), params=params, **kwargs)");
        Console.WriteLine("        response.raise_for_status()");
        Console.WriteLine("        return response.json()");
        Console.WriteLine("    ");
        Console.WriteLine("    def post(self, endpoint, data=None, json=None, **kwargs):");
        Console.WriteLine("        response = self.session.post(");
        Console.WriteLine("            self._url(endpoint), data=data, json=json, **kwargs");
        Console.WriteLine("        )");
        Console.WriteLine("        response.raise_for_status()");
        Console.WriteLine("        return response.json()");
        Console.WriteLine("    ");
        Console.WriteLine("    def put(self, endpoint, data=None, json=None, **kwargs):");
        Console.WriteLine("        response = self.session.put(");
        Console.WriteLine("            self._url(endpoint), data=data, json=json, **kwargs");
        Console.WriteLine("        )");
        Console.WriteLine("        response.raise_for_status()");
        Console.WriteLine("        return response.json()");
        Console.WriteLine("    ");
        Console.WriteLine("    def delete(self, endpoint, **kwargs):");
        Console.WriteLine("        response = self.session.delete(self._url(endpoint), **kwargs)");
        Console.WriteLine("        response.raise_for_status()");
        Console.WriteLine("        return response.status_code == 204");
        
        Console.WriteLine("\n# Usage example");
        Console.WriteLine("client = RESTClient(");
        Console.WriteLine("    'https://api.example.com',");
        Console.WriteLine("    headers={'Authorization': 'Bearer token123'}");
        Console.WriteLine(")");
        Console.WriteLine("# GET request");
        Console.WriteLine("users = client.get('/users', params={'page': 1})");
        Console.WriteLine("# POST request");
        Console.WriteLine("new_user = client.post('/users', json={'name': 'John', 'email': 'john@example.com'})");
        
        Console.WriteLine("\n# Resource-specific client");
        Console.WriteLine("class UserClient:");
        Console.WriteLine("    def __init__(self, rest_client):");
        Console.WriteLine("        self.client = rest_client");
        Console.WriteLine("    ");
        Console.WriteLine("    def list_users(self, page=1, limit=10):");
        Console.WriteLine("        return self.client.get('/users', params={'page': page, 'limit': limit})");
        Console.WriteLine("    ");
        Console.WriteLine("    def get_user(self, user_id):");
        Console.WriteLine("        return self.client.get(f'/users/{user_id}')");
        Console.WriteLine("    ");
        Console.WriteLine("    def create_user(self, user_data):");
        Console.WriteLine("        return self.client.post('/users', json=user_data)");
        Console.WriteLine("    ");
        Console.WriteLine("    def update_user(self, user_id, user_data):");
        Console.WriteLine("        return self.client.put(f'/users/{user_id}', json=user_data)");
        Console.WriteLine("    ");
        Console.WriteLine("    def delete_user(self, user_id):");
        Console.WriteLine("        return self.client.delete(f'/users/{user_id}')");
        
        Console.WriteLine("\n# Pagination helper");
        Console.WriteLine("def paginate_all(client, endpoint, page_param='page', limit_param='limit', limit=100):");
        Console.WriteLine("    \"\"\"Fetch all pages from a paginated endpoint\"\"\"");
        Console.WriteLine("    all_items = []");
        Console.WriteLine("    page = 1");
        Console.WriteLine("    ");
        Console.WriteLine("    while True:");
        Console.WriteLine("        params = {page_param: page, limit_param: limit}");
        Console.WriteLine("        response = client.get(endpoint, params=params)");
        Console.WriteLine("        ");
        Console.WriteLine("        items = response.get('items', [])");
        Console.WriteLine("        if not items:");
        Console.WriteLine("            break");
        Console.WriteLine("            ");
        Console.WriteLine("        all_items.extend(items)");
        Console.WriteLine("        ");
        Console.WriteLine("        # Check if we've reached the last page");
        Console.WriteLine("        if len(items) < limit:");
        Console.WriteLine("            break");
        Console.WriteLine("            ");
        Console.WriteLine("        page += 1");
        Console.WriteLine("    ");
        Console.WriteLine("    return all_items");
    }

    /// <summary>
    /// GraphQL API integration patterns
    /// </summary>
    private static void GraphQLIntegration()
    {
        Console.WriteLine("\n6. GraphQL Integration:");
        
        Console.WriteLine("# Basic GraphQL query");
        Console.WriteLine("import requests");
        Console.WriteLine("import json");
        Console.WriteLine("");
        Console.WriteLine("def execute_graphql_query(url, query, variables=None, headers=None):");
        Console.WriteLine("    payload = {'query': query}");
        Console.WriteLine("    if variables:");
        Console.WriteLine("        payload['variables'] = variables");
        Console.WriteLine("    ");
        Console.WriteLine("    response = requests.post(");
        Console.WriteLine("        url,");
        Console.WriteLine("        json=payload,");
        Console.WriteLine("        headers=headers or {}");
        Console.WriteLine("    )");
        Console.WriteLine("    response.raise_for_status()");
        Console.WriteLine("    ");
        Console.WriteLine("    result = response.json()");
        Console.WriteLine("    if 'errors' in result:");
        Console.WriteLine("        raise Exception(f'GraphQL errors: {result[\"errors\"]}')");
        Console.WriteLine("    ");
        Console.WriteLine("    return result['data']");
        
        Console.WriteLine("\n# GraphQL queries");
        Console.WriteLine("# Simple query");
        Console.WriteLine("query = '''");
        Console.WriteLine("query GetUsers {");
        Console.WriteLine("  users {");
        Console.WriteLine("    id");
        Console.WriteLine("    name");
        Console.WriteLine("    email");
        Console.WriteLine("  }");
        Console.WriteLine("}");
        Console.WriteLine("'''");
        
        Console.WriteLine("\n# Query with variables");
        Console.WriteLine("query_with_variables = '''");
        Console.WriteLine("query GetUser($userId: ID!) {");
        Console.WriteLine("  user(id: $userId) {");
        Console.WriteLine("    id");
        Console.WriteLine("    name");
        Console.WriteLine("    email");
        Console.WriteLine("    posts {");
        Console.WriteLine("      id");
        Console.WriteLine("      title");
        Console.WriteLine("      content");
        Console.WriteLine("    }");
        Console.WriteLine("  }");
        Console.WriteLine("}");
        Console.WriteLine("'''");
        Console.WriteLine("variables = {'userId': '123'}");
        
        Console.WriteLine("\n# Mutation");
        Console.WriteLine("mutation = '''");
        Console.WriteLine("mutation CreateUser($input: CreateUserInput!) {");
        Console.WriteLine("  createUser(input: $input) {");
        Console.WriteLine("    id");
        Console.WriteLine("    name");
        Console.WriteLine("    email");
        Console.WriteLine("  }");
        Console.WriteLine("}");
        Console.WriteLine("'''");
        Console.WriteLine("variables = {");
        Console.WriteLine("    'input': {");
        Console.WriteLine("        'name': 'John Doe',");
        Console.WriteLine("        'email': 'john@example.com'");
        Console.WriteLine("    }");
        Console.WriteLine("}");
        
        Console.WriteLine("\n# GraphQL client class");
        Console.WriteLine("class GraphQLClient:");
        Console.WriteLine("    def __init__(self, url, headers=None):");
        Console.WriteLine("        self.url = url");
        Console.WriteLine("        self.headers = headers or {}");
        Console.WriteLine("    ");
        Console.WriteLine("    def execute(self, query, variables=None):");
        Console.WriteLine("        return execute_graphql_query(self.url, query, variables, self.headers)");
        Console.WriteLine("    ");
        Console.WriteLine("    def query(self, query, variables=None):");
        Console.WriteLine("        \"\"\"Execute a query operation\"\"\"");
        Console.WriteLine("        return self.execute(query, variables)");
        Console.WriteLine("    ");
        Console.WriteLine("    def mutate(self, mutation, variables=None):");
        Console.WriteLine("        \"\"\"Execute a mutation operation\"\"\"");
        Console.WriteLine("        return self.execute(mutation, variables)");
        
        Console.WriteLine("\n# Using graphql-core for query building");
        Console.WriteLine("from graphql import build_schema, validate, parse");
        Console.WriteLine("# Schema definition");
        Console.WriteLine("schema_definition = '''");
        Console.WriteLine("type Query {");
        Console.WriteLine("  user(id: ID!): User");
        Console.WriteLine("  users: [User]");
        Console.WriteLine("}");
        Console.WriteLine("type User {");
        Console.WriteLine("  id: ID!");
        Console.WriteLine("  name: String!");
        Console.WriteLine("  email: String!");
        Console.WriteLine("}");
        Console.WriteLine("'''");
    }

    /// <summary>
    /// Webhook handling patterns
    /// </summary>
    private static void WebhookHandling()
    {
        Console.WriteLine("\n7. Webhook Handling:");
        
        Console.WriteLine("from flask import Flask, request, jsonify");
        Console.WriteLine("import hashlib");
        Console.WriteLine("import hmac");
        Console.WriteLine("import json");
        
        Console.WriteLine("\n# Basic webhook endpoint");
        Console.WriteLine("app = Flask(__name__)");
        Console.WriteLine("");
        Console.WriteLine("@app.route('/webhook', methods=['POST'])");
        Console.WriteLine("def handle_webhook():");
        Console.WriteLine("    data = request.json");
        Console.WriteLine("    print(f'Received webhook: {data}')");
        Console.WriteLine("    ");
        Console.WriteLine("    # Process the webhook data");
        Console.WriteLine("    process_webhook_data(data)");
        Console.WriteLine("    ");
        Console.WriteLine("    return jsonify({'status': 'success'}), 200");
        
        Console.WriteLine("\n# Webhook signature verification");
        Console.WriteLine("def verify_signature(payload, signature, secret):");
        Console.WriteLine("    \"\"\"Verify webhook signature for security\"\"\"");
        Console.WriteLine("    expected_signature = hmac.new(");
        Console.WriteLine("        secret.encode('utf-8'),");
        Console.WriteLine("        payload.encode('utf-8'),");
        Console.WriteLine("        hashlib.sha256");
        Console.WriteLine("    ).hexdigest()");
        Console.WriteLine("    ");
        Console.WriteLine("    return hmac.compare_digest(f'sha256={expected_signature}', signature)");
        Console.WriteLine("");
        Console.WriteLine("@app.route('/secure-webhook', methods=['POST'])");
        Console.WriteLine("def handle_secure_webhook():");
        Console.WriteLine("    signature = request.headers.get('X-Hub-Signature-256')");
        Console.WriteLine("    payload = request.get_data(as_text=True)");
        Console.WriteLine("    ");
        Console.WriteLine("    if not verify_signature(payload, signature, WEBHOOK_SECRET):");
        Console.WriteLine("        return jsonify({'error': 'Invalid signature'}), 401");
        Console.WriteLine("    ");
        Console.WriteLine("    data = json.loads(payload)");
        Console.WriteLine("    process_webhook_data(data)");
        Console.WriteLine("    return jsonify({'status': 'success'}), 200");
        
        Console.WriteLine("\n# Webhook retry mechanism");
        Console.WriteLine("import time");
        Console.WriteLine("from datetime import datetime, timedelta");
        Console.WriteLine("def send_webhook(url, data, secret=None, max_retries=3):");
        Console.WriteLine("    \"\"\"Send webhook with retry logic\"\"\"");
        Console.WriteLine("    headers = {'Content-Type': 'application/json'}");
        Console.WriteLine("    ");
        Console.WriteLine("    if secret:");
        Console.WriteLine("        payload = json.dumps(data)");
        Console.WriteLine("        signature = hmac.new(");
        Console.WriteLine("            secret.encode('utf-8'),");
        Console.WriteLine("            payload.encode('utf-8'),");
        Console.WriteLine("            hashlib.sha256");
        Console.WriteLine("        ).hexdigest()");
        Console.WriteLine("        headers['X-Hub-Signature-256'] = f'sha256={signature}'");
        Console.WriteLine("    ");
        Console.WriteLine("    for attempt in range(max_retries + 1):");
        Console.WriteLine("        try:");
        Console.WriteLine("            response = requests.post(url, json=data, headers=headers, timeout=10)");
        Console.WriteLine("            response.raise_for_status()");
        Console.WriteLine("            return True");
        Console.WriteLine("        except Exception as e:");
        Console.WriteLine("            if attempt == max_retries:");
        Console.WriteLine("                print(f'Failed to send webhook after {max_retries + 1} attempts: {e}')");
        Console.WriteLine("                return False");
        Console.WriteLine("            ");
        Console.WriteLine("            wait_time = 2 ** attempt");
        Console.WriteLine("            print(f'Webhook attempt {attempt + 1} failed, retrying in {wait_time}s')");
        Console.WriteLine("            time.sleep(wait_time)");
        
        Console.WriteLine("\n# Webhook queue system");
        Console.WriteLine("import redis");
        Console.WriteLine("from rq import Queue");
        Console.WriteLine("redis_conn = redis.Redis()");
        Console.WriteLine("webhook_queue = Queue('webhooks', connection=redis_conn)");
        Console.WriteLine("");
        Console.WriteLine("def queue_webhook(url, data):");
        Console.WriteLine("    \"\"\"Queue webhook for background processing\"\"\"");
        Console.WriteLine("    webhook_queue.enqueue(send_webhook, url, data)");
        Console.WriteLine("");
        Console.WriteLine("def process_webhook_queue():");
        Console.WriteLine("    \"\"\"Worker function to process webhook queue\"\"\"");
        Console.WriteLine("    while True:");
        Console.WriteLine("        job = webhook_queue.dequeue()");
        Console.WriteLine("        if job:");
        Console.WriteLine("            job.perform()");
        Console.WriteLine("        else:");
        Console.WriteLine("            time.sleep(1)");
    }

    /// <summary>
    /// API testing patterns
    /// </summary>
    private static void APITestingPatterns()
    {
        Console.WriteLine("\n8. API Testing Patterns:");
        
        Console.WriteLine("import pytest");
        Console.WriteLine("import requests_mock");
        Console.WriteLine("from unittest.mock import patch, Mock");
        
        Console.WriteLine("\n# Basic API test");
        Console.WriteLine("def test_get_user():");
        Console.WriteLine("    with requests_mock.Mocker() as m:");
        Console.WriteLine("        m.get('https://api.example.com/users/1', json={'id': 1, 'name': 'John'})");
        Console.WriteLine("        ");
        Console.WriteLine("        response = requests.get('https://api.example.com/users/1')");
        Console.WriteLine("        data = response.json()");
        Console.WriteLine("        ");
        Console.WriteLine("        assert response.status_code == 200");
        Console.WriteLine("        assert data['id'] == 1");
        Console.WriteLine("        assert data['name'] == 'John'");
        
        Console.WriteLine("\n# Test error scenarios");
        Console.WriteLine("def test_api_error_handling():");
        Console.WriteLine("    with requests_mock.Mocker() as m:");
        Console.WriteLine("        # Test 404 error");
        Console.WriteLine("        m.get('https://api.example.com/users/999', status_code=404)");
        Console.WriteLine("        ");
        Console.WriteLine("        response = requests.get('https://api.example.com/users/999')");
        Console.WriteLine("        assert response.status_code == 404");
        Console.WriteLine("        ");
        Console.WriteLine("        # Test timeout");
        Console.WriteLine("        m.get('https://api.example.com/slow', exc=requests.Timeout)");
        Console.WriteLine("        ");
        Console.WriteLine("        with pytest.raises(requests.Timeout):");
        Console.WriteLine("            requests.get('https://api.example.com/slow')");
        
        Console.WriteLine("\n# Integration test with test client");
        Console.WriteLine("class TestAPIIntegration:");
        Console.WriteLine("    def setup_method(self):");
        Console.WriteLine("        self.client = RESTClient('https://api.example.com')");
        Console.WriteLine("    ");
        Console.WriteLine("    @patch('requests.Session.get')");
        Console.WriteLine("    def test_get_users(self, mock_get):");
        Console.WriteLine("        mock_response = Mock()");
        Console.WriteLine("        mock_response.json.return_value = {'users': [{'id': 1, 'name': 'John'}]}");
        Console.WriteLine("        mock_response.raise_for_status.return_value = None");
        Console.WriteLine("        mock_get.return_value = mock_response");
        Console.WriteLine("        ");
        Console.WriteLine("        result = self.client.get('/users')");
        Console.WriteLine("        ");
        Console.WriteLine("        assert 'users' in result");
        Console.WriteLine("        assert len(result['users']) == 1");
        
        Console.WriteLine("\n# Performance testing");
        Console.WriteLine("import time");
        Console.WriteLine("def test_api_performance():");
        Console.WriteLine("    start_time = time.time()");
        Console.WriteLine("    ");
        Console.WriteLine("    response = requests.get('https://api.example.com/users')");
        Console.WriteLine("    ");
        Console.WriteLine("    end_time = time.time()");
        Console.WriteLine("    response_time = end_time - start_time");
        Console.WriteLine("    ");
        Console.WriteLine("    assert response.status_code == 200");
        Console.WriteLine("    assert response_time < 2.0  # Should respond within 2 seconds");
        
        Console.WriteLine("\n# Contract testing with Pact");
        Console.WriteLine("from pact import Consumer, Provider");
        Console.WriteLine("pact = Consumer('UserService').has_pact_with(Provider('UserAPI'))");
        Console.WriteLine("");
        Console.WriteLine("def test_user_contract():");
        Console.WriteLine("    expected = {");
        Console.WriteLine("        'id': 1,");
        Console.WriteLine("        'name': 'John Doe',");
        Console.WriteLine("        'email': 'john@example.com'");
        Console.WriteLine("    }");
        Console.WriteLine("    ");
        Console.WriteLine("    (pact");
        Console.WriteLine("     .given('User 1 exists')");
        Console.WriteLine("     .upon_receiving('a request for user 1')");
        Console.WriteLine("     .with_request('GET', '/users/1')");
        Console.WriteLine("     .will_respond_with(200, body=expected))");
        Console.WriteLine("    ");
        Console.WriteLine("    with pact:");
        Console.WriteLine("        response = requests.get(f'{pact.uri}/users/1')");
        Console.WriteLine("        assert response.json() == expected");
    }
}