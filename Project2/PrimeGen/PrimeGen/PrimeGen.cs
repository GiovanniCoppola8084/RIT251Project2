/*
 * The purpose of this program is to find and search through a random list of set size BigIntegers and check if they are
 * prime or not. There will only be a certain number of them printed. Both the size of the BitInteger (in bits) and the
 * number of primes to be printed will be provided in the command line arguments. The algorithm to determine if the
 * number is prime was taken from the pseudocode in the project instructions and added as a BigInteger extension method.
 *
 * @author Giovanni Coppola (gac6151@rit.edu)
 */

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Security.Cryptography;
using CustomExtensions;

namespace PrimeGen
{
    /// <summary>
    /// This class will be what handles the command line arguments and will call the method to find the prime numbers
    /// using parallel programming.
    /// </summary>
    class Program
    {
        /// <summary>
        /// The main function will process the command line arguments and call the function to find the prime numbers.
        /// </summary>
        /// <param name="args">The list of command line arguments.</param>
        static void Main(string[] args)
        {
            // Make sure that there the correct number of arguments
            if (args.Length is < 0 or > 3)
            {
                // Print a usage statement and exit the code
                PrintUsage();
                Environment.Exit(0);
            }
            // Check to make sure the bits are within the correct constraints
            if (Convert.ToInt32(args[0]) % 8 != 0 && Convert.ToInt32(args[0]) < 32)
            {
                PrintUsage();
                Environment.Exit(0);
            }

            // Make the object of FindPrimeNumbers based on the command line arguments
            FindPrimeNumbers primeNumbers;
            if (args.Length == 1)
            {
                primeNumbers = new FindPrimeNumbers(Convert.ToInt32(args[0]), 1);
            }
            else
            {
                primeNumbers = new FindPrimeNumbers(Convert.ToInt32(args[0]), Convert.ToInt32(args[1]));
            }
            
            // Call the function to generate the prime numbers
            primeNumbers.GeneratePrimeNumbers();
        }

        /// <summary>
        /// This method will print the usage statement for the command line arguments if the correct ones are not
        /// entered.
        /// </summary>
        private static void PrintUsage()
        {
            Console.WriteLine("Usage: PrimeGen <bits> <count=1>");
            Console.WriteLine("Must have the first parameter. The second is optional.");
            Console.WriteLine("<bits>       The maximum number of bits to run the program on.");
            Console.WriteLine("             (Must be a multiple of 8 and greater than 32)");
            Console.WriteLine("<count=1>    The number of prime numbers that will be found.");
            Console.WriteLine("             (The default will be 1)");
        }
    }

    /// <summary>
    /// This class will be used to find the numbers and check if they are prime using parallel programming. The method
    /// to check if they are prime, however, will be called as an extension method on the BigInteger class.
    /// </summary>
    class FindPrimeNumbers
    {
        private int NumberOfBits { get; set; }
        private int CountOfPrimes { get; set; }

        /// <summary>
        /// Constructor method for the FindPrimeNumbers class
        /// </summary>
        /// <param name="numberOfBits">The number of bits that the number must be.</param>
        /// <param name="countOfPrimes">The number of prime numbers that will be printed out.</param>
        public FindPrimeNumbers(int numberOfBits, int countOfPrimes)
        {
            NumberOfBits = numberOfBits;
            CountOfPrimes = countOfPrimes;
        }
        
        /// <summary>
        /// The method will generate the random numbers in parallel and check if they are prime
        /// </summary>
        public void GeneratePrimeNumbers()
        {
            var numOfPrimesFound = 0;
            var myLock = new object();
            
            Console.WriteLine("BitLength: {0}", NumberOfBits);
            
            // Set up the random number generator to find the correct size integers at random
            var random = RandomNumberGenerator.Create();
            var bytes = new byte[(NumberOfBits/4)];
            
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            // Run a parallel loop to find if a number is prime or not
            Parallel.For(0, int.MaxValue, (_, parallelLoopState) =>
            {
                // Stop the threads if the correct number of primes have already been printed
                if (numOfPrimesFound == CountOfPrimes)
                {
                    parallelLoopState.Stop();
                }

                if (parallelLoopState.IsStopped)
                {
                    return;
                }

                // Get the random bytes and create a big integer out of it at random
                random.GetBytes(bytes);
                var number = new BigInteger(bytes);
                //Console.WriteLine(number);
                // Set up a lock on the console print since it would not be thread safe without it
                lock (myLock)
                {
                    // Check the factors of the number to rule out options that we know will already not work
                    // This will include 2 and some of the more common known prime factors
                    if (BigInteger.Abs(number) < 0 || BigInteger.Abs(number) % 2 == 0 || 
                        BigInteger.Abs(number) % 3 == 0 || BigInteger.Abs(number) % 5 == 0 || 
                        BigInteger.Abs(number) % 7 == 0 /*|| BigInteger.Abs(number) % 11 == 0 || 
                        BigInteger.Abs(number) % 13 == 0 || BigInteger.Abs(number) % 17 == 0*/) return;
                    // Check if the number is prime and if the correct number of primes have already been printed
                    if (!number.IsProbablyPrime(10) || parallelLoopState.IsStopped ||
                        numOfPrimesFound == CountOfPrimes) return;
                    // Print the number if it is found to be prime
                    Console.WriteLine("{0}: {1}", numOfPrimesFound + 1, number);
                    Interlocked.Increment(ref numOfPrimesFound);
                }
            });

            stopWatch.Stop();
            Console.WriteLine("Time to Generate: {0:hh\\:mm\\:ss\\.ffff}", stopWatch.Elapsed);
        }
    }
}

namespace CustomExtensions
{
    /// <summary>
    /// This class is used to store the extension methods on the BigInteger class (specifically the one that we made to
    /// check if the given BigInteger is a prime number).
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// This was created by the teacher and we had to copy it into the code. This extension method will determine if
        /// the number that we found is prime or not.
        /// </summary>
        /// <param name="value">The integer being checked</param>
        /// <param name="k">The amount of rounds it will be tested in</param>
        /// <returns>If the number is prime or not</returns>
        public static bool IsProbablyPrime(this BigInteger value, int k = 10)
        {
            if (value <= 1) return false;
            if (k <= 0) k = 10;

            BigInteger d = value - 1;
            var s = 0;

            while (d % 2 == 0)
            {
                d /= 2;
                s += 1;
            }

            var bytes = new byte[value.ToByteArray().LongLength];
            BigInteger a;

            for (var i = 0; i < k; i++)
            {
                do
                {
                    var Gen = new Random();
                    Gen.NextBytes(bytes);
                    a = new BigInteger(bytes);
                } while (a < 2 || a >= value - 2);
                
                var x = BigInteger.ModPow(a, d, value);
                if (x == 1 || x == (value - 1)) continue;
                for (var r = 1; r < s; r++)
                {
                    x = BigInteger.ModPow(x, 2, value);
                    if (x == 1) return false;
                    if (x == value - 1) break;
                }

                if (x != value - 1) return false;
            }

            return true;
        }
            
    }
}