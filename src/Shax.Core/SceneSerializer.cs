using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shax;

public static class SceneSerializer
{
    public sealed class SceneDto
    {
        public int FormatVersion { get; set; } = 1;
        public string Name { get; set; } = "Scene";
        public List<EntityDto> Entities { get; set; } = new();
    }
    public sealed class EntityDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "Entity";
        public bool Active { get; set; }
        public Guid? ParentId { get; set; }
        public float[] Position { get; set; } = new float[3];
        public float[] Rotation { get; set; } = new float[] { 0,0,0,1 };
        public float[] Scale { get; set; } = new float[] { 1,1,1 };
    }
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.Strict
    };

    public static string Serialize(Scene scene)
    {
        if (scene is null) throw new ArgumentNullException(nameof(scene));
        var dto = new SceneDto { Name = scene.Name };
        dto.Entities = scene.Entities.Select(e => new EntityDto
        {
            Id = e.Id, Name = e.Name, Active = e.Active, ParentId = e.Parent?.Id,
            Position = new[] { e.Transform.LocalPosition.X, e.Transform.LocalPosition.Y, e.Transform.LocalPosition.Z },
            Rotation = new[] { e.Transform.LocalRotation.X, e.Transform.LocalRotation.Y, e.Transform.LocalRotation.Z, e.Transform.LocalRotation.W },
            Scale = new[] { e.Transform.LocalScale.X, e.Transform.LocalScale.Y, e.Transform.LocalScale.Z }
        }).ToList();
        return JsonSerializer.Serialize(dto, Options);
    }

    public static Scene Deserialize(string json)
    {
        var dto = JsonSerializer.Deserialize<SceneDto>(json, Options)
            ?? throw new FormatException("Scene JSON is empty or invalid.");
        if (dto.FormatVersion != 1) throw new NotSupportedException($"Scene format {dto.FormatVersion} is not supported.");
        var scene = new Scene(dto.Name);
        var entities = new Dictionary<Guid, Entity>();
        foreach (var item in dto.Entities)
        {
            if (item.Id == Guid.Empty || entities.ContainsKey(item.Id))
                throw new FormatException("Scene entity IDs must be unique, non-empty GUIDs.");
            var entity = new Entity(item.Name, item.Id) { Active = item.Active };
            var p = ReadVector(item.Position, 3, "position");
            entity.Transform.LocalPosition = new Vector3(p[0], p[1], p[2]);
            var q = ReadVector(item.Rotation, 4, "rotation");
            var rotation = new Quaternion(q[0], q[1], q[2], q[3]);
            if (rotation.LengthSquared() < 1e-8f) throw new FormatException("Invalid zero rotation quaternion.");
            entity.Transform.LocalRotation = Quaternion.Normalize(rotation);
            var scale = ReadVector(item.Scale, 3, "scale");
            entity.Transform.LocalScale = new Vector3(scale[0], scale[1], scale[2]);
            scene.AddEntity(entity);
            entities.Add(item.Id, entity);
        }
        foreach (var item in dto.Entities)
        {
            if (item.ParentId is Guid parentId)
            {
                if (!entities.TryGetValue(parentId, out var parent))
                    throw new FormatException($"Missing parent entity {parentId}.");
                entities[item.Id].SetParent(parent);
            }
        }
        return scene;
    }

    private static float[] ReadVector(float[]? values, int count, string label)
    {
        if (values is null || values.Length != count || values.Any(v => float.IsNaN(v) || float.IsInfinity(v)))
            throw new FormatException($"Invalid {label} vector.");
        return values;
    }
}
