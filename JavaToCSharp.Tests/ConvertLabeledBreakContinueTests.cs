namespace JavaToCSharp.Tests;

public class ConvertLabeledBreakContinueTests
{
    private const string NestedLoops = """
                                       package com.example;
                                       public class Grid {
                                           public void scan() {
                                               outer: for (int i = 0; i < 4; i++) {
                                                   for (int j = 0; j < 4; j++) {
                                                       if (j == 1) {
                                                           continue outer;
                                                       }
                                                       if (i == 3) {
                                                           break outer;
                                                       }
                                                   }
                                               }
                                           }
                                       }
                                       """;

    [Fact]
    public void Labeled_Jumps_Use_CSharp15_Syntax_By_Default()
    {
        var warnings = new List<string>();
        var parsed = Convert(NestedLoops, NewOptions(warnings));

        Assert.Contains("outer:", parsed);
        Assert.Contains("break outer;", parsed);
        Assert.Contains("continue outer;", parsed);
        Assert.DoesNotContain("goto", parsed);
        Assert.Empty(warnings);
    }

    [Fact]
    public void Labeled_Jumps_Fall_Back_To_Goto_When_Option_Disabled()
    {
        var warnings = new List<string>();
        var parsed = Convert(NestedLoops, NewOptions(warnings, useLabeledJumps: false));

        Assert.Contains("goto outer_break;", parsed);
        Assert.Contains("goto outer_continue;", parsed);
        Assert.Contains("outer_break:", parsed);
        Assert.Contains("outer_continue:", parsed);
        Assert.DoesNotContain("break outer;", parsed);
        Assert.DoesNotContain("continue outer;", parsed);
        Assert.Empty(warnings);
    }

    [Fact]
    public void Goto_Fallback_Emits_Continue_Target_Inside_Loop_And_Break_Target_After()
    {
        var parsed = Convert(NestedLoops, NewOptions(useLabeledJumps: false));

        // The continue target must precede the break target: it belongs to the end of the loop body,
        // while the break target must follow the loop entirely.
        int continueTarget = parsed.IndexOf("outer_continue:", StringComparison.Ordinal);
        int breakTarget = parsed.IndexOf("outer_break:", StringComparison.Ordinal);

        Assert.True(continueTarget > 0 && breakTarget > 0);
        Assert.True(continueTarget < breakTarget);
    }

    [Fact]
    public void Goto_Fallback_Only_Emits_Targets_That_Are_Used()
    {
        const string breakOnly = """
                                 package com.example;
                                 public class Grid {
                                     public void scan() {
                                         outer: for (int i = 0; i < 4; i++) {
                                             for (int j = 0; j < 4; j++) {
                                                 if (j == 1) {
                                                     break outer;
                                                 }
                                             }
                                         }
                                     }
                                 }
                                 """;

        var parsed = Convert(breakOnly, NewOptions(useLabeledJumps: false));

        Assert.Contains("outer_break:", parsed);
        Assert.DoesNotContain("outer_continue", parsed);
    }

    [Fact]
    public void Unlabeled_Jumps_Are_Unaffected()
    {
        const string unlabeled = """
                                 package com.example;
                                 public class Grid {
                                     public void scan() {
                                         for (int i = 0; i < 4; i++) {
                                             if (i == 1) {
                                                 continue;
                                             }
                                             if (i == 3) {
                                                 break;
                                             }
                                         }
                                     }
                                 }
                                 """;

        foreach (bool useLabeledJumps in new[] { true, false })
        {
            var parsed = Convert(unlabeled, NewOptions(useLabeledJumps: useLabeledJumps));

            Assert.Contains("break;", parsed);
            Assert.Contains("continue;", parsed);
            Assert.DoesNotContain("goto", parsed);
        }
    }

    [Fact]
    public void Labeled_Continue_On_While_Loop_Is_Lowered_Into_Loop_Body()
    {
        const string whileLoop = """
                                 package com.example;
                                 public class Counter {
                                     public void count() {
                                         int k = 0;
                                         loop: while (k < 5) {
                                             k++;
                                             if (k < 5) {
                                                 continue loop;
                                             }
                                             break loop;
                                         }
                                     }
                                 }
                                 """;

        var parsed = Convert(whileLoop, NewOptions(useLabeledJumps: false));

        int loopEnd = parsed.IndexOf("loop_break:", StringComparison.Ordinal);
        int continueTarget = parsed.IndexOf("loop_continue:", StringComparison.Ordinal);

        // The continue target belongs inside the while body, so it appears before the break target.
        Assert.True(continueTarget > 0);
        Assert.True(continueTarget < loopEnd);
    }

    [Fact]
    public void Nested_Labels_Each_Get_Their_Own_Targets()
    {
        const string nestedLabels = """
                                    package com.example;
                                    public class Grid {
                                        public void scan() {
                                            outer: for (int i = 0; i < 4; i++) {
                                                inner: for (int j = 0; j < 4; j++) {
                                                    if (j == 1) {
                                                        break inner;
                                                    }
                                                    if (i == 3) {
                                                        break outer;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    """;

        var parsed = Convert(nestedLabels, NewOptions(useLabeledJumps: false));

        Assert.Contains("goto inner_break;", parsed);
        Assert.Contains("goto outer_break;", parsed);
        Assert.Contains("inner_break:", parsed);
        Assert.Contains("outer_break:", parsed);
    }

    private static JavaConversionOptions NewOptions(List<string>? warnings = null, bool useLabeledJumps = true)
    {
        var options = new JavaConversionOptions
        {
            IncludeComments = false,
            UseLabeledBreakAndContinue = useLabeledJumps,
        };
        options.WarningEncountered += (_, eventArgs) => warnings?.Add(eventArgs.Message);
        return options;
    }

    private static string Convert(string javaCode, JavaConversionOptions? options = null)
        => JavaToCSharpConverter.ConvertText(javaCode, options ?? NewOptions()) ?? "";
}
