//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
using System.Text;

namespace Jeninnet.Validation.ContractFingerprint;

internal static class Program {
    public static int Main(string[] args) {
        try {
            if(args.Contains("--self-test", StringComparer.Ordinal)) {
                return SelfTest.Run();
            }

            var output = ReadOutputPath(args);
            var json = FingerprintDocument.CreateJson();

            if(output is null) {
                Console.Out.Write(json);
            } else {
                var directory = Path.GetDirectoryName(output);

                if(!string.IsNullOrEmpty(directory)) {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(output, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }

            return 0;
        }
        catch(Exception exception) {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static string? ReadOutputPath(string[] args) {
        for(var index = 0; index < args.Length; index++) {
            if(string.Equals(args[index], "--output", StringComparison.Ordinal) &&
                index + 1 < args.Length) {
                return args[index + 1];
            }
        }

        return null;
    }
}
