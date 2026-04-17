using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Linq;

public class ExportFullProjectReport
{
    [MenuItem("Tools/Export FULL Project Report")]
    public static void ExportReport()
    {
        StringBuilder report = new StringBuilder();

        report.AppendLine("UNITY PROJECT REPORT");
        report.AppendLine("====================================");
        report.AppendLine();

        AddProjectInfo(report);
        AddFolderStructure(report);
        AddScenes(report);
        AddScripts(report);
        AddPlugins(report);
        AddScriptableObjects(report);
        AddBuildSettings(report);

        string path = Path.Combine(Application.dataPath, "../UnityProjectReport.txt");
        File.WriteAllText(path, report.ToString());

        Debug.Log("Project report exported: " + path);
        EditorUtility.RevealInFinder(path);
    }

    static void AddProjectInfo(StringBuilder report)
    {
        report.AppendLine("PROJECT INFO");
        report.AppendLine("------------------------------------");
        report.AppendLine("Unity version: " + Application.unityVersion);
        report.AppendLine("Project name: " + Application.productName);
        report.AppendLine("Platform: " + EditorUserBuildSettings.activeBuildTarget);
        report.AppendLine();
    }

    static void AddFolderStructure(StringBuilder report)
    {
        report.AppendLine("FOLDER STRUCTURE (Assets)");
        report.AppendLine("------------------------------------");

        string path = Application.dataPath;
        PrintDirectory(path, report, 0);

        report.AppendLine();
    }

    static void PrintDirectory(string path, StringBuilder report, int indent)
    {
        string indentStr = new string(' ', indent * 2);
        string name = Path.GetFileName(path);

        report.AppendLine(indentStr + name + "/");

        foreach (var dir in Directory.GetDirectories(path))
        {
            if (dir.Contains("Library") || dir.Contains(".git"))
                continue;

            PrintDirectory(dir, report, indent + 1);
        }

        foreach (var file in Directory.GetFiles(path))
        {
            if (file.EndsWith(".meta"))
                continue;

            report.AppendLine(indentStr + "  " + Path.GetFileName(file));
        }
    }

    static void AddScenes(StringBuilder report)
    {
        report.AppendLine("SCENES");
        report.AppendLine("------------------------------------");

        var scenes = AssetDatabase.FindAssets("t:Scene");

        foreach (var guid in scenes)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            report.AppendLine(path);
        }

        report.AppendLine();
    }

    static void AddScripts(StringBuilder report)
    {
        report.AppendLine("SCRIPTS");
        report.AppendLine("------------------------------------");

        var scripts = AssetDatabase.FindAssets("t:MonoScript");

        foreach (var guid in scripts)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            report.AppendLine(path);
        }

        report.AppendLine();
    }

    static void AddPlugins(StringBuilder report)
    {
        report.AppendLine("PLUGINS");
        report.AppendLine("------------------------------------");

        string pluginPath = "Assets/Plugins";

        if (Directory.Exists(pluginPath))
        {
            var dirs = Directory.GetDirectories(pluginPath);

            foreach (var dir in dirs)
            {
                report.AppendLine(dir);
            }
        }

        report.AppendLine();
    }

    static void AddScriptableObjects(StringBuilder report)
    {
        report.AppendLine("SCRIPTABLE OBJECTS");
        report.AppendLine("------------------------------------");

        var assets = AssetDatabase.FindAssets("t:ScriptableObject");

        foreach (var guid in assets)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            report.AppendLine(path);
        }

        report.AppendLine();
    }

    static void AddBuildSettings(StringBuilder report)
    {
        report.AppendLine("BUILD SETTINGS");
        report.AppendLine("------------------------------------");

        foreach (var scene in EditorBuildSettings.scenes)
        {
            report.AppendLine(scene.path);
        }

        report.AppendLine();
    }
}