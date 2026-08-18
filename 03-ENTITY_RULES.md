# AriaHR — Domain Entity Rules

These rules are mandatory for all Domain Entities in AriaHR.

## General Rules

- Follow the existing AriaHR Master Development Standard.
- Follow the Modular Monolith architecture.
- Do not invent fields.
- Do not remove requested fields.
- Do not rename requested fields.
- Do not add business logic.
- Do not add validation.
- Do not add UI-related code.
- Do not add DataAnnotations.
- Do not add unnecessary abstractions.

## Entity Structure

- Every Domain Entity must inherit from `BaseEntity`.
- Every Entity must be a pure POCO.
- Use PascalCase.
- Use Guid identifiers.
- The primary key is inherited from `BaseEntity.Id`.
- Foreign key properties must end with `Id`.
- Do not create constructors unless explicitly requested.
- Do not create methods unless explicitly requested.
- Do not create business logic.

## Properties

- Create only explicitly requested properties.
- Do not add additional properties.
- Do not remove requested properties.
- Do not change requested property types.
- Nullable properties must be nullable exactly when specified.
- Do not add navigation properties unless explicitly requested.

## Attributes

Do NOT use:

- [Required]
- [MaxLength]
- [MinLength]
- [Display]
- [DisplayName]
- [UIHint]
- [ScaffoldColumn]
- Any other UI or validation attributes.

Use Fluent API for persistence configuration when configuration is explicitly requested.

## XML Documentation

- XML documentation is allowed only for the Entity class.
- Do not add XML documentation to individual properties.
- Do not add unnecessary comments.

## Architecture

- Entities belong only to their own module.
- Do not reference entities from other business modules.
- Do not create cross-module entity dependencies.
- Do not create repositories.
- Do not create services.
- Do not create DTOs.
- Do not create DbContexts.
- Do not create EF configurations unless explicitly requested.

## Output

- One Entity per file.
- File name must match the Entity name.
- Namespace must match the module's Domain namespace.
- Create only the requested Entity.
- Do not create additional files.

## Uncertainty Rule

If any requested field, type, relationship, namespace, inheritance, or architectural requirement is unclear:

STOP and ask for clarification.

Do not guess.
Do not invent.
Do not make architectural assumptions.