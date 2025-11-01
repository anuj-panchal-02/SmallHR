# 🔒 Multi-Tenant Penetration Test Checklist

This document provides a comprehensive test matrix and sample HTTP requests for testing multi-tenant isolation and security.

---

## Test Matrix

| Test ID | Test Case | Expected Result | Severity |
|---------|-----------|-----------------|----------|
| **MT-001** | Cross-Tenant Data Access via ID Enumeration | 403 Forbidden | Critical |
| **MT-002** | Tenant Spoofing via Header Manipulation | 403 Forbidden | Critical |
| **MT-003** | Bypassing Tenant Validation | 403 Forbidden | Critical |
| **MT-004** | Accessing Other Tenant's Resources via Direct ID | 403 Forbidden | Critical |
| **MT-005** | JWT Token Reuse Across Tenants | 403 Forbidden | High |
| **MT-006** | SQL Injection via Tenant ID | 400 Bad Request | High |
| **MT-007** | Tenant ID Injection in Query Parameters | 400 Bad Request | Medium |
| **MT-008** | Missing Tenant Header Fallback | Default tenant assigned | Medium |
| **MT-009** | Tenant Claim Mismatch | 403 Forbidden | High |
| **MT-010** | Race Condition in Tenant Resolution | Consistent tenant isolation | High |

---

## Test 1: MT-001 - Cross-Tenant Data Access via ID Enumeration

### Objective
Verify that users from Tenant A cannot access resources from Tenant B by ID enumeration.

### Setup
1. Create two tenants: `tenant1` and `tenant2`
2. Create user `user1@tenant1.com` with role `Admin` in `tenant1`
3. Create user `user2@tenant2.com` with role `Admin` in `tenant2`
4. Create employee `EMP001` in `tenant1` and `EMP002` in `tenant2`

### Test Steps

#### Step 1: Login as Tenant1 User

```http
POST http://localhost:5192/api/auth/login
Content-Type: application/json

{
  "email": "user1@tenant1.com",
  "password": "Tenant1@123"
}
```

**Expected Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "...",
  "expiration": "2025-01-28T10:00:00Z",
  "user": {
    "id": "user1-id",
    "email": "user1@tenant1.com",
    "roles": ["Admin"],
    "tenant": "tenant1"
  }
}
```

#### Step 2: Access Own Tenant's Employee (Should Succeed)

```http
GET http://localhost:5192/api/employees/1
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
X-Tenant-Id: tenant1
```

**Expected Response:** `200 OK`
```json
{
  "id": 1,
  "employeeId": "EMP001",
  "firstName": "John",
  "lastName": "Doe",
  "tenantId": "tenant1"
}
```

#### Step 3: Attempt to Access Other Tenant's Employee (Should Fail)

```http
GET http://localhost:5192/api/employees/2
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
X-Tenant-Id: tenant1
```

**Expected Response:** `403 Forbidden`
```json
{
  "message": "Access denied: Employee belongs to different tenant"
}
```

### Automated Test

```csharp
[Fact]
public async Task Should_Reject_Cross_Tenant_Resource_Access()
{
    // Arrange
    var tenant1Token = await GetTokenForTenant("tenant1");
    var tenant2EmployeeId = await CreateEmployeeInTenant("tenant2", "EMP002");

    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", tenant1Token);
    client.DefaultRequestHeaders.Add("X-Tenant-Id", "tenant1");

    // Act
    var response = await client.GetAsync($"/api/employees/{tenant2EmployeeId}");

    // Assert
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}
```

---

## Test 2: MT-002 - Tenant Spoofing via Header Manipulation

### Objective
Verify that users cannot spoof tenant IDs via HTTP headers to access other tenants' data.

### Test Steps

#### Step 1: Login as Tenant1 User

```http
POST http://localhost:5192/api/auth/login
Content-Type: application/json

{
  "email": "user1@tenant1.com",
  "password": "Tenant1@123"
}
```

#### Step 2: Attempt to Access Tenant2's Data by Spoofing Header

```http
GET http://localhost:5192/api/employees
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
X-Tenant-Id: tenant2
```

**Expected Response:** `403 Forbidden`
```json
{
  "message": "Tenant mismatch. JWT tenant claim does not match requested tenant."
}
```

### Automated Test

```csharp
[Fact]
public async Task Should_Reject_Tenant_Spoofing_Attempt()
{
    // Arrange
    var tenant1Token = await GetTokenForTenant("tenant1");
    
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", tenant1Token);
    client.DefaultRequestHeaders.Add("X-Tenant-Id", "tenant2"); // Spoofing tenant

    // Act
    var response = await client.GetAsync("/api/employees");

    // Assert
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}
```

---

## Test 3: MT-003 - Bypassing Tenant Validation

### Objective
Verify that tenant validation cannot be bypassed by omitting tenant headers or using invalid tenant IDs.

### Test Steps

#### Step 1: Access Resource Without Tenant Header

```http
GET http://localhost:5192/api/employees
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Expected Response:** `200 OK` (defaults to user's tenant from JWT claim)

#### Step 2: Access Resource with Invalid Tenant ID

```http
GET http://localhost:5192/api/employees
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
X-Tenant-Id: invalid-tenant-999
```

**Expected Response:** `403 Forbidden`
```json
{
  "message": "Invalid tenant"
}
```

### Automated Test

```csharp
[Fact]
public async Task Should_Reject_Invalid_Tenant_Id()
{
    // Arrange
    var token = await GetTokenForTenant("tenant1");
    
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);
    client.DefaultRequestHeaders.Add("X-Tenant-Id", "invalid-tenant-999");

    // Act
    var response = await client.GetAsync("/api/employees");

    // Assert
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}
```

---

## Test 4: MT-004 - Accessing Other Tenant's Resources via Direct ID

### Objective
Verify that direct resource access by ID is scoped to the user's tenant.

### Test Steps

#### Step 1: Attempt to Update Other Tenant's Employee

```http
PUT http://localhost:5192/api/employees/2
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
X-Tenant-Id: tenant1
Content-Type: application/json

{
  "firstName": "Hacked",
  "lastName": "User"
}
```

**Expected Response:** `403 Forbidden`
```json
{
  "message": "Access denied: Employee belongs to different tenant"
}
```

#### Step 2: Attempt to Delete Other Tenant's Employee

```http
DELETE http://localhost:5192/api/employees/2
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
X-Tenant-Id: tenant1
```

**Expected Response:** `403 Forbidden`

### Automated Test

```csharp
[Fact]
public async Task Should_Reject_Cross_Tenant_Update()
{
    // Arrange
    var tenant1Token = await GetTokenForTenant("tenant1");
    var tenant2EmployeeId = await CreateEmployeeInTenant("tenant2", "EMP002");
    
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", tenant1Token);
    client.DefaultRequestHeaders.Add("X-Tenant-Id", "tenant1");

    var updateRequest = new UpdateEmployeeRequest
    {
        FirstName = "Hacked",
        LastName = "User"
    };

    // Act
    var response = await client.PutAsJsonAsync(
        $"/api/employees/{tenant2EmployeeId}", updateRequest);

    // Assert
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}
```

---

## Test 5: MT-005 - JWT Token Reuse Across Tenants

### Objective
Verify that JWT tokens cannot be reused across different tenants.

### Test Steps

#### Step 1: Login as Tenant1 User and Get Token

```http
POST http://localhost:5192/api/auth/login
Content-Type: application/json

{
  "email": "user1@tenant1.com",
  "password": "Tenant1@123"
}
```

**Response:** Token with `tenant: tenant1` claim

#### Step 2: Attempt to Use Token with Tenant2 Header

```http
GET http://localhost:5192/api/employees
Authorization: Bearer <tenant1-token>
X-Tenant-Id: tenant2
```

**Expected Response:** `403 Forbidden`
```json
{
  "message": "Tenant mismatch."
}
```

### Automated Test

```csharp
[Fact]
public async Task Should_Reject_Token_Reuse_Across_Tenants()
{
    // Arrange
    var tenant1Token = await GetTokenForTenant("tenant1");
    
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", tenant1Token);
    client.DefaultRequestHeaders.Add("X-Tenant-Id", "tenant2");

    // Act
    var response = await client.GetAsync("/api/employees");

    // Assert
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}
```

---

## Test 6: MT-006 - SQL Injection via Tenant ID

### Objective
Verify that SQL injection attempts via tenant ID are blocked.

### Test Steps

#### Step 1: Attempt SQL Injection in Tenant Header

```http
GET http://localhost:5192/api/employees
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
X-Tenant-Id: tenant1'; DROP TABLE Employees; --
```

**Expected Response:** `400 Bad Request` or `403 Forbidden`
```json
{
  "message": "Invalid tenant format"
}
```

### Automated Test

```csharp
[Theory]
[InlineData("tenant1'; DROP TABLE Employees; --")]
[InlineData("tenant1' OR '1'='1")]
[InlineData("tenant1' UNION SELECT * FROM Users--")]
public async Task Should_Reject_Sql_Injection_In_Tenant_Id(string maliciousTenantId)
{
    // Arrange
    var token = await GetTokenForTenant("tenant1");
    
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);
    client.DefaultRequestHeaders.Add("X-Tenant-Id", maliciousTenantId);

    // Act
    var response = await client.GetAsync("/api/employees");

    // Assert
    Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
}
```

---

## Test 7: MT-007 - Tenant ID Injection in Query Parameters

### Objective
Verify that tenant ID injection in query parameters is sanitized.

### Test Steps

#### Step 1: Attempt Tenant Injection via Query Parameter

```http
GET http://localhost:5192/api/employees?tenantId=tenant2
Authorization: Bearer <tenant1-token>
X-Tenant-Id: tenant1
```

**Expected Behavior:** Query parameter ignored; tenant ID from header/JWT used

**Expected Response:** `200 OK` (only tenant1 employees)

### Automated Test

```csharp
[Fact]
public async Task Should_Ignore_Tenant_Id_In_Query_Parameters()
{
    // Arrange
    var tenant1Token = await GetTokenForTenant("tenant1");
    await CreateEmployeeInTenant("tenant2", "EMP002");
    
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", tenant1Token);
    client.DefaultRequestHeaders.Add("X-Tenant-Id", "tenant1");

    // Act
    var response = await client.GetAsync("/api/employees?tenantId=tenant2");
    var employees = await response.Content.ReadFromJsonAsync<List<EmployeeDto>>();

    // Assert
    Assert.True(response.IsSuccessStatusCode);
    Assert.All(employees!, emp => Assert.Equal("tenant1", emp.TenantId));
}
```

---

## Test 8: MT-008 - Missing Tenant Header Fallback

### Objective
Verify that missing tenant header falls back to default tenant or JWT claim.

### Test Steps

#### Step 1: Access Resource Without Tenant Header

```http
GET http://localhost:5192/api/employees
Authorization: Bearer <token-with-tenant-claim>
```

**Expected Response:** `200 OK` (tenant from JWT claim used)

#### Step 2: Access Resource Without Tenant Header or JWT

```http
GET http://localhost:5192/api/employees
```

**Expected Response:** `401 Unauthorized`

### Automated Test

```csharp
[Fact]
public async Task Should_Use_JWT_Tenant_Claim_When_Header_Missing()
{
    // Arrange
    var token = await GetTokenForTenant("tenant1");
    
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);
    // No X-Tenant-Id header

    // Act
    var response = await client.GetAsync("/api/employees");
    var employees = await response.Content.ReadFromJsonAsync<List<EmployeeDto>>();

    // Assert
    Assert.True(response.IsSuccessStatusCode);
    Assert.All(employees!, emp => Assert.Equal("tenant1", emp.TenantId));
}
```

---

## Test 9: MT-009 - Tenant Claim Mismatch

### Objective
Verify that JWT tenant claim must match resolved tenant ID.

### Test Steps

#### Step 1: Create Token with Tenant1 Claim

```http
POST http://localhost:5192/api/auth/login
Content-Type: application/json

{
  "email": "user1@tenant1.com",
  "password": "Tenant1@123"
}
```

**Response:** Token with `tenant: tenant1` in claims

#### Step 2: Attempt to Access with Tenant2 Header

```http
GET http://localhost:5192/api/employees
Authorization: Bearer <token-with-tenant1-claim>
X-Tenant-Id: tenant2
```

**Expected Response:** `403 Forbidden`
```json
{
  "message": "Tenant mismatch."
}
```

### Automated Test

```csharp
[Fact]
public async Task Should_Reject_Tenant_Claim_Mismatch()
{
    // Arrange
    var tenant1Token = await GetTokenForTenant("tenant1");
    
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", tenant1Token);
    client.DefaultRequestHeaders.Add("X-Tenant-Id", "tenant2");

    // Act
    var response = await client.GetAsync("/api/employees");

    // Assert
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}
```

---

## Test 10: MT-010 - Race Condition in Tenant Resolution

### Objective
Verify that concurrent requests maintain consistent tenant isolation.

### Test Steps

#### Step 1: Send Concurrent Requests from Different Tenants

```http
# Request 1 (Tenant 1)
GET http://localhost:5192/api/employees
Authorization: Bearer <tenant1-token>
X-Tenant-Id: tenant1

# Request 2 (Tenant 2) - Concurrent
GET http://localhost:5192/api/employees
Authorization: Bearer <tenant2-token>
X-Tenant-Id: tenant2
```

**Expected Behavior:** Each request returns only its tenant's data, even when concurrent

### Automated Test

```csharp
[Fact]
public async Task Should_Maintain_Tenant_Isolation_Under_Concurrency()
{
    // Arrange
    var tenant1Token = await GetTokenForTenant("tenant1");
    var tenant2Token = await GetTokenForTenant("tenant2");
    
    await CreateEmployeeInTenant("tenant1", "EMP001");
    await CreateEmployeeInTenant("tenant2", "EMP002");

    var client1 = _factory.CreateClient();
    client1.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", tenant1Token);
    client1.DefaultRequestHeaders.Add("X-Tenant-Id", "tenant1");

    var client2 = _factory.CreateClient();
    client2.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", tenant2Token);
    client2.DefaultRequestHeaders.Add("X-Tenant-Id", "tenant2");

    // Act - Concurrent requests
    var tasks = new[]
    {
        client1.GetAsync("/api/employees"),
        client2.GetAsync("/api/employees"),
        client1.GetAsync("/api/employees"),
        client2.GetAsync("/api/employees")
    };
    
    var responses = await Task.WhenAll(tasks);
    var results = await Task.WhenAll(responses.Select(r => 
        r.Content.ReadFromJsonAsync<List<EmployeeDto>>()));

    // Assert
    foreach (var (response, employees) in responses.Zip(results))
    {
        Assert.True(response.IsSuccessStatusCode);
        var tenantId = response.RequestMessage!.Headers.GetValues("X-Tenant-Id").First();
        Assert.All(employees!, emp => Assert.Equal(tenantId, emp.TenantId));
    }
}
```

---

## Summary

Run these tests before each release:

1. **Automated Tests:** Run `dotnet test --filter "Category=TenantIsolation"`
2. **Manual Tests:** Use the HTTP requests above with Postman/curl
3. **Security Tools:** Run Burp Suite or OWASP ZAP for additional testing

**Expected Results:** All tests should pass with proper tenant isolation enforced.

