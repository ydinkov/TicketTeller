# TicketTeller
<img src="Logo.png" width="200">

## Overview

This service is a .NET minimal API built to manage credit-pased subscriptions with overage.
The service provides endpoints to create, read, update, and delete subscriptions, as well as use and refresh credit tokens for a subscription.
The service also provides a report of a subscription showing the number of exhausted, expired but not exhausted and overage tokens for that subscription.

The API uses Entity Framework Core with a choice of SQL Server, MySQL, PostgreSQL, or SQLite databases, configurable via environment variables.

## Glossary

- **Subscription**: A subscription in this context is an entity that signifies a particular service plan that a user has signed up for. It contains information such as the subscription ID, the name, description, ticket lifetime, metadata, start date, end date, refresh interval and refresh amount, and next refresh date.

- **TicketToken**: A ticket token represents a usage token for a subscription. It has a unique ID, a reference to the subscription it belongs to, a creation timestamp, an expiration date, an optional subject, an optional exhausted date, and an overage flag that is initially set to false.

- **ExhaustedDate**: The date and time at which a ticket token is considered exhausted or used up.

- **Overage**: This represents a situation where a ticket token has to be created because no available tokens were found for a subscription. The overage flag in a ticket token is set to true in such a situation.

- **Subject**: In the context of a ticket token, the subject refers to an optional string that gets assigned when a ticket token is exhausted.

- **Refresh**: This process involves creating new ticket tokens for a subscription when the next refresh date is due.

- **API Keys**: These are secret tokens used for authentication. In this application, there are three pairs of primary and secondary API keys. One pair for admin operations (full access), one pair for contributor operations (can CRUD subscriptions and request reports) and one pair for user operations (can only request to exhaust a token from a subscription).

## Setup

### Requirements
- .NET 6.0+
- Docker
- Docker Compose

### Installation

1. Clone the repository:
   ```
   git clone https://github.com/your-username/subscription-api.git
   ```
2. Move into the project directory:
   ```
   cd subscription-api
   ```
3. Build the Docker image and start the container alongside a PostgreSQL container:
   ```
   docker-compose up --build
   ```

### Configuration

The application can be configured using the following environment variables:

- `DB_TYPE`: The type of the database. Options are `sqlserver`, `mysql`, `postgres`, `sqlite`.
- `DB_CONNECTION_STRING`: The connection string to the database.
- `ADMIN_API_KEY` : Can access all endpoints 
- `CONTRIBUTOR_API_KEY` : Can Create and edit subscriptions
- `USER_API_KEY` : Can only execute Use endpoint

The API provides several endpoints:

Sure, here are example `curl` commands for each endpoint:

- **Get all subscriptions**
  ```
  curl -X GET http://localhost:5000/subscriptions -H "Api-Key: your-api-key"
  ```

- **Get a specific subscription**
  Replace `{id}` with the id of the subscription you want to get.
  ```
  curl -X GET http://localhost:5000/subscriptions/{id} -H "Api-Key: your-api-key"
  ```

- **Create a new subscription**
  Replace `{...}` with your JSON payload.
  ```
  curl -X POST http://localhost:5000/subscriptions -H "Api-Key: your-api-key" -H "Content-Type: application/json" -d '{...}'
  ```

- **Update a subscription**
  Replace `{id}` with the id of the subscription you want to update and `{...}` with your JSON payload.
  ```
  curl -X PUT http://localhost:5000/subscriptions/{id} -H "Api-Key: your-api-key" -H "Content-Type: application/json" -d '{...}'
  ```

- **Delete a subscription**
  Replace `{id}` with the id of the subscription you want to delete.
  ```
  curl -X DELETE http://localhost:5000/subscriptions/{id} -H "Api-Key: your-api-key"
  ```

- **Use a ticket of a subscription**
  Replace `{id}` with the id of the subscription you want to use and `{subject}` with your subject.
  ```
  curl -X POST http://localhost:5000/subscriptions/{id}/use -H "Api-Key: your-api-key" -H "Content-Type: application/json" -d '{ "subject": "{subject}" }'
  ```

- **Get a report of a subscription**
  Replace `{id}` with the id of the subscription you want to get a report of.
  ```
  curl -X GET http://localhost:5000/subscriptions/{id}/report -H "Api-Key: your-api-key"
  ```

Remember to replace `your-api-key` with the API key that's appropriate for the operation you're trying to perform.