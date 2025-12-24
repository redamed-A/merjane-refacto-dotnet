# Merjane Refactoring

## Objective
Refactor to improve code structure while ensuring no regression.

## Approach
- Add characterization tests for product availability
- Extract policy classes for product types and Replace conditional logic with Strategy Pattern (SRP)
-  Add Dependency Inversion Principle for ProductService and OrderService + Use async methods
- Added comprehensive unit tests covering all scenarios for Normal, Seasonal, and Expirable products and renaming it to ProductServiceTests
- Renaming + cleanup and add update Readme
- Ensure all tests pass after each change

## Improvements
- Better separation of concerns (SRP)
- Single Responsibility Principle
- Asynchronous methods fot performance
- Easier to read and maintain
- Easier to extend for new product types

## Tests
Use `dotnet test` to run the full suite.

## Next Steps
- Add more edge-case tests
- Add logging
- Add metrics