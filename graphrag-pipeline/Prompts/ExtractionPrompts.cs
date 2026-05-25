namespace GraphRag.Pipeline.Prompts;

public static class ExtractionPrompts
{
    public const string TupleDelimiter = "<|>";
    public const string RecordDelimiter = "\n";
    public const string CompletionDelimiter = "<|COMPLETE|>";

    public static string BuildExtractionPrompt(string text, string[] entityTypes) =>
        $"""
        -Goal-
        Given a text document and a list of entity types, identify all entities of those types and all relationships among the identified entities.

        -Steps-
        1. Identify all entities. For each identified entity, extract:
        - entity_name: Name of the entity, capitalized
        - entity_type: One of the following types: [{string.Join(", ", entityTypes)}]
        - entity_description: Comprehensive description of the entity's attributes and activities
        Format each entity as: ("entity"{TupleDelimiter}<entity_name>{TupleDelimiter}<entity_type>{TupleDelimiter}<entity_description>)

        2. Identify all pairs of (source_entity, target_entity) that are clearly related. For each pair extract:
        - source_entity: name of the source entity (must match an entity from step 1)
        - target_entity: name of the target entity (must match an entity from step 1)
        - relationship_description: explanation of why they are related
        - relationship_strength: numeric score from 1 to 10
        Format each relationship as: ("relationship"{TupleDelimiter}<source_entity>{TupleDelimiter}<target_entity>{TupleDelimiter}<relationship_description>{TupleDelimiter}<relationship_strength>)

        3. Output all entities and relationships as a flat list separated by {RecordDelimiter}.
        4. When finished, output {CompletionDelimiter}

        -Real Data-
        Entity_types: {string.Join(", ", entityTypes)}
        Text: {text}

        Output:
        """;

    public const string GleaningPrompt =
        "MANY entities were missed in the last extraction. List them below using the same format. " +
        "Output DONE if there are no more entities to add.";
}
