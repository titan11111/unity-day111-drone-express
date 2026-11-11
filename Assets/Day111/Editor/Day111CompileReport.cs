using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Day111Editor
{
    // どのアセンブリがコンパイルエラーでも、結果を Verification/compile-report.txt に書き出す
    [InitializeOnLoad]
    public static class Day111CompileReport
    {
        static readonly StringBuilder sb = new StringBuilder();
        static string ReportPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Verification/compile-report.txt"));

        static Day111CompileReport()
        {
            CompilationPipeline.compilationStarted += _ => sb.Clear();
            CompilationPipeline.assemblyCompilationFinished += OnAsm;
            CompilationPipeline.compilationFinished += _ => Flush();
        }

        static void OnAsm(string asm, CompilerMessage[] msgs)
        {
            int err = 0;
            foreach (var m in msgs) if (m.type == CompilerMessageType.Error) err++;
            sb.AppendLine("[" + System.IO.Path.GetFileName(asm) + "] errors=" + err);
            foreach (var m in msgs)
                if (m.type == CompilerMessageType.Error || m.type == CompilerMessageType.Warning)
                    sb.AppendLine("  " + m.type + " " + m.file + "(" + m.line + "): " + m.message);
        }

        static void Flush()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
                File.WriteAllText(ReportPath, System.DateTime.Now.ToString("s") + "\n" + sb);
            }
            catch { }
        }
    }
}
