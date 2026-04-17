using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public class ExportProjectStructure
{
    [MenuItem("Tools/Export Project Structure")]
    public static void ExportStructure()
    {
        string assetsPath = Application.dataPath;
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("Unity Project Structure");
        sb.AppendLine("=======================");
        sb.AppendLine();

        ProcessDirectory(assetsPath, sb, 0);

        string outputPath = Path.Combine(Application.dataPath, "../project_structure.txt");
        File.WriteAllText(outputPath, sb.ToString());

        Debug.Log("Project structure exported to: " + outputPath);
        EditorUtility.RevealInFinder(outputPath);
    }

    static void ProcessDirectory(string path, StringBuilder sb, int indent)
    {
        string indentStr = new string(' ', indent * 2);
        string folderName = Path.GetFileName(path);

        sb.AppendLine($"{indentStr}{folderName}/");

        // folders
        foreach (var dir in Directory.GetDirectories(path))
        {
            if (dir.Contains("Library") || dir.Contains(".git"))
                continue;

            ProcessDirectory(dir, sb, indent + 1);
        }

        // files
        foreach (var file in Directory.GetFiles(path))
        {
            if (file.EndsWith(".meta"))
                continue;

            string fileName = Path.GetFileName(file);
            sb.AppendLine($"{indentStr}  {fileName}");
        }
    }
}