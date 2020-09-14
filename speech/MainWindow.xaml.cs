using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.CognitiveServices.Speech;

namespace speech
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    
        public partial class MainWindow : Window
        {
            private static string text;

            public MainWindow() 
            {
                InitializeComponent();
                Main();
                
            }

            static async Task Main()
            {
                String input;
                Console.WriteLine("Enter 1 to start, enter 0 to exit.\n");
                input = Console.ReadLine();
                while (input == "1")
                {
                    await RecognitionWithMicrophoneAsync();
                    await SynthesisToSpeakerAsync();
                    Console.WriteLine("Enter 1 to start, enter 0 to exit.\n");
                    input = Console.ReadLine();
                }
                Console.WriteLine("Bye.\n");

            }

        // Speech synthesis to the default speaker.
        public static async Task SynthesisToSpeakerAsync()
        {
            String YourSubscriptionKey = "5974cf0f9354417c93fe8ff98d998227";
            String YourServiceRegion = "westus";

            var config = SpeechConfig.FromSubscription(YourSubscriptionKey, YourServiceRegion);

            if (text == "How are you?")
            {
                text = "I'm fine, thank you.";
            }
            else if (text == "What's your name?")
            {
                text = "It's a secret.";
            }
            else if (text == "Can you help me?")
            {
                text = "My pleasure.";
            }
            else
            {
                text = "Pardon?";
            }

            using (var synthesizer = new SpeechSynthesizer(config))
            {
                while (true)
                {
                    if (string.IsNullOrEmpty(text))
                    {
                        break;
                    }

                    using (var result = await synthesizer.SpeakTextAsync(text))
                    {
                        if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                        {
                            Console.WriteLine($"Speech synthesized to speaker for text [{text}]");
                        }
                        else if (result.Reason == ResultReason.Canceled)
                        {
                            var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                            Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                            if (cancellation.Reason == CancellationReason.Error)
                            {
                                Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                                Console.WriteLine($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");
                                Console.WriteLine($"CANCELED: Did you update the subscription info?");
                            }
                        }
                        break;
                    }
                }
            }
        }

        // Speech recognition from microphone.
        public static async Task RecognitionWithMicrophoneAsync()
            {
                String YourSubscriptionKey = "5974cf0f9354417c93fe8ff98d998227";
                String YourServiceRegion = "westus";

                var config = SpeechConfig.FromSubscription(YourSubscriptionKey, YourServiceRegion);

                using (var recognizer = new SpeechRecognizer(config))
                {
                    Console.WriteLine("Say something...");

                    var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);

                    if (result.Reason == ResultReason.RecognizedSpeech)
                    {
                        Console.WriteLine($"RECOGNIZED: Text={result.Text}");
                        text = result.Text;
                }
                    else if (result.Reason == ResultReason.NoMatch)
                    {
                        Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                    }
                    else if (result.Reason == ResultReason.Canceled)
                    {
                        var cancellation = CancellationDetails.FromResult(result);
                        Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                        if (cancellation.Reason == CancellationReason.Error)
                        {
                            Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                            Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                            Console.WriteLine($"CANCELED: Did you update the subscription info?");
                        }
                    }
                }

            }

        }
    
}
