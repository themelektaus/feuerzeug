using NiflySharp;
using NiflySharp.Blocks;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Feuerzeug;

public class NifInstance()
{
    public static void BulkModify(
        string texturesDataPath,
        string meshesInputPath,
        string meshesOutputPath
    )
    {
        if (string.IsNullOrEmpty(texturesDataPath))
        {
            Logger.Error($"Textures Data Path is not set");
            return;
        }

        if (string.IsNullOrEmpty(meshesInputPath))
        {
            Logger.Error($"Meshes Input Path is not set");
            return;
        }

        if (string.IsNullOrEmpty(meshesOutputPath))
        {
            Logger.Error($"Meshes Output Path is not set");
            return;
        }

        texturesDataPath = Path.GetFullPath(texturesDataPath);
        meshesInputPath = Path.GetFullPath(meshesInputPath);
        meshesOutputPath = Path.GetFullPath(meshesOutputPath);

        if (Directory.Exists(meshesOutputPath) && (Directory.GetFiles(meshesOutputPath).Length != 0 || Directory.GetDirectories(meshesOutputPath).Length != 0))
        {
            Logger.Error($"Meshes Output Path is not empty");
            return;
        }

        var files = Directory.EnumerateFiles(
            meshesInputPath,
            "*.nif",
            SearchOption.AllDirectories
        );

        foreach (var file in files)
        {
            var nif = new NifInstance
            {
                TexturesDataPath = texturesDataPath,
                MeshesInputPath = meshesInputPath,
                MeshesOutputPath = meshesOutputPath
            };

            if (!nif.Load(file))
            {
                Logger.Warning($"Could not load: {file}");
                continue;
            }

            var modifications = nif.Modify();
            if (modifications.Count > 0)
            {
                var message = new StringBuilder();
                message.AppendLine(file);

                foreach (var (before, after) in modifications)
                {
                    message.AppendLine($" => before: {before}");
                    message.AppendLine($"     after: {after}");
                }

                Logger.Result(message.ToString());

                if (!nif.Save())
                {
                    Logger.Error($"Could not save: {file}");
                }
            }
        }
    }

    public string TexturesDataPath { get; set; }
    public string MeshesInputPath { get; set; }
    public string MeshesOutputPath { get; set; }

    string fileName;

    readonly NifFile nif = new();
    readonly List<BSShaderTextureSet> textureSets = [];


    public bool Load(string fileName)
    {
        this.fileName = fileName;

        if (nif.Load(fileName) != 0)
        {
            return false;
        }

        foreach (var block in nif.Blocks)
        {
            if (block is BSShaderTextureSet shaderTextureSet)
            {
                textureSets.Add(shaderTextureSet);
            }
        }

        return true;
    }

    public List<(string before, string after)> Modify()
    {
        var result = new List<(string before, string after)>();

        foreach (var textureSet in textureSets)
        {
            var textures = textureSet.Textures;
            if (textures.Count >= 2)
            {
                var before = textures[1].Content;

                if (!string.IsNullOrEmpty(textures[0].Content))
                {
                    var x = textures[0].Content.Split('\\');
                    var name = Path.GetFileNameWithoutExtension(x[^1]);
                    var after = $"{string.Join('\\', x[..^1])}\\{name}_n.dds";
                    if (File.Exists(Path.Combine(TexturesDataPath, after)))
                    {
                        if (before != after)
                        {
                            result.Add((before, after));
                            textures[1].Content = after;
                        }
                    }
                }
            }
        }

        return result;
    }

    public bool Save()
    {
        var fileName = MeshesOutputPath
            + this.fileName[MeshesInputPath.Length..];

        Directory.CreateDirectory(Path.GetDirectoryName(fileName));

        return nif.Save(fileName) == 0;
    }
}
