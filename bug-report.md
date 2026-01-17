## Description

The Shiny Mediator OpenAPI client source generator fails to compile when generating code for PUT/POST endpoints that have both:
1. A path parameter (e.g., `{Id}` in route)
2. A request body with properties

The generator creates code that references a variable `Id` that doesn't exist in scope, resulting in compilation error `CS0103: The name 'Id' does not exist in the current context`.

## Expected Behavior

For a handler like this:

```csharp
[MediatorHttpPut("/api/properties/{Id}", OperationId = "UpdateProperty")]
public async Task<UpdatePropertyResponse> Handle(UpdatePropertyRequest request, ...)
{
    // Handler implementation
}

public record UpdatePropertyRequest(
    string Title,
    string Address,
    // ... other properties
) : IRequest<UpdatePropertyResponse>;
```

The generator should:
1. Generate OpenAPI spec with `Id` as a **path parameter** (in `parameters` array)
2. Generate client code that properly extracts `Id` from the route

## Actual Behavior

### 1. Generated OpenAPI Spec (WRONG)

```json
{
  "paths": {
    "/api/properties/{Id}": {
      "put": {
        "operationId": "UpdateProperty",
        "requestBody": {
          "content": {
            "application/json": {
              "schema": {
                "$ref": "#/components/schemas/UpdatePropertyRequest"
              }
            }
          }
        }
      }
    }
  },
  "components": {
    "schemas": {
      "UpdatePropertyRequest": {
        "properties": {
          "title": { "type": "string" },
          "address": { "type": "string" }
        }
      }
    }
  }
}
```

**Problem:** No `parameters` array for the path parameter `{Id}`.

### 2. Generated Client Code (COMPILATION ERROR)

```csharp
public partial class UpdatePropertyHttpRequestHandler
{
    public Task<UpdatePropertyResponse> Handle(
        UpdatePropertyHttpRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var route = $"/api/properties/{Id}";  // ❌ ERROR: 'Id' does not exist

        var httpRequest = this.CreateHttpRequest(context, HttpMethod.Put, route);

        if (request.Body != null)
        {
            var json = services.Serializer.Serialize(request.Body);
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return this.HandleRequest<...>(...);
    }
}

public partial class UpdatePropertyHttpRequest : IRequest<UpdatePropertyResponse>
{
    public required UpdatePropertyRequest Body { get; set; }  // ❌ No Id property!
}
```

**Compilation Error:**
```
error CS0103: The name 'Id' does not exist in the current context
```

## Comparison with DELETE (Works Correctly)

For DELETE endpoints with only a path parameter, the generator works correctly:

```csharp
public record DeletePropertyRequest(Guid Id) : IRequest<DeletePropertyResponse>;
```

**Generated OpenAPI Spec (CORRECT):**
```json
{
  "delete": {
    "operationId": "DeleteProperty",
    "parameters": [
      {
        "name": "Id",
        "in": "path",
        "required": true,
        "schema": { "type": "string", "format": "uuid" }
      }
    ]
  }
}
```

**Generated Client Code (COMPILES):**
```csharp
public partial class DeletePropertyHttpRequest
{
    public required Guid Id { get; set; }  // ✅ Id property exists!
}

public partial class DeletePropertyHttpRequestHandler
{
    public Task<DeletePropertyResponse> Handle(...)
    {
        var route = $"/api/properties/{request.Id}";  // ✅ Works!
        ...
    }
}
```

## Reproduction Steps

1. Create a handler with PUT/POST and path parameter in route but not in request:
   ```csharp
   [MediatorHttpPut("/api/items/{Id}", OperationId = "UpdateItem")]
   public async Task<ItemResponse> Handle(UpdateItemRequest request, ...)

   public record UpdateItemRequest(
       string Name,       // Body
       string Description // Body
   ) : IRequest<ItemResponse>;
   ```

2. Build the project with the OpenAPI client generator
3. Observe compilation error in generated code

## Attempted Workarounds (None Work)

### ❌ Attempt 1: Include Id in request
```csharp
public record UpdatePropertyRequest(
    Guid Id,      // Try to match route parameter
    string Title,
    ...
) : IRequest<UpdatePropertyResponse>;
```
**Result:** All properties (including Id) end up in request body schema, no `parameters` array generated

### ❌ Attempt 2: Use `[HttpParameter]` and `[HttpBody]` attributes
```csharp
public class UpdatePropertyRequest : IRequest<UpdatePropertyResponse>
{
    [HttpParameter(HttpParameterType.Path)]
    public Guid Id { get; set; }

    [HttpBody]
    public UpdatePropertyDto Data { get; set; }
}
```
**Result:** Attributes are ignored by OpenAPI generator; all properties still in body

### ❌ Attempt 3: Extract Id from RouteValues in handler
```csharp
public record UpdatePropertyRequest(
    // No Id property
    string Title,
    ...
) : IRequest<UpdatePropertyResponse>;

// In handler:
var id = httpContext.Request.RouteValues["Id"];
```
**Result:** Client generator still tries to use `{Id}` in route template without an Id variable

## Environment

- **Shiny.Mediator version:** 6.1.0 (tested also with 6.0.2)
- **Shiny.Mediator.SourceGenerators version:** 6.1.0
- **.NET version:** 10.0
- **Target frameworks:** net10.0 (server), net10.0-desktop (client)

## Root Cause Analysis

The OpenAPI generator appears to:
1. Generate `parameters` array **only** when the request has **exclusively** path/query parameters
2. Place **all properties** in the request body schema when the request has **any** body content

This creates a mismatch:
- Route template: `/api/properties/{Id}` expects an `Id` variable
- Generated client: No `Id` property exists to fill the route template

## Suggested Fix

The OpenAPI generator should detect path parameters from the route template (e.g., `{Id}`) and:
1. **NOT** include them in the request body schema
2. Generate them as path parameters in the `parameters` array
3. Generate client code with both path parameter properties AND body property

**Expected Generated Client:**
```csharp
public partial class UpdatePropertyHttpRequest
{
    public required Guid Id { get; set; }                // From path
    public required UpdatePropertyDto Body { get; set; }  // From body
}

public partial class UpdatePropertyHttpRequestHandler
{
    public Task<UpdatePropertyResponse> Handle(...)
    {
        var route = $"/api/properties/{request.Id}";  // ✅ Id exists!
        // ... set body content
    }
}
```

## Impact

This bug makes it impossible to use the generated OpenAPI client for any PUT/PATCH/POST endpoint that needs both:
- Resource identification via path parameter (standard REST pattern)
- Update data via request body

This is a very common pattern in REST APIs and currently requires manual HTTP client implementation as a workaround.
