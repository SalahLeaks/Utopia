using Apollo.Service;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.UObject;
using System.Threading.Tasks;

namespace Apollo.Utils
{
    public static class ProviderUtils
    {
        /// <summary>
        /// Asynchronously loads an object of type T from the specified package path.
        /// </summary>
        /// <typeparam name="T">The type of UObject to load.</typeparam>
        /// <param name="packagePath">The path to the package file.</param>
        /// <returns>A task that represents the asynchronous load operation.</returns>
        public static async Task<T> LoadObjectAsync<T>(string packagePath) where T : UObject
        {
            return await ApplicationService.CUE4Parse.Provider.LoadPackageObjectAsync<T>(packagePath).ConfigureAwait(false);
        }

        /// <summary>
        /// Synchronously loads an object of type T from the specified package path.
        /// </summary>
        /// <typeparam name="T">The type of UObject to load.</typeparam>
        /// <param name="packagePath">The path to the package file.</param>
        /// <returns>The loaded object.</returns>
        public static T LoadObject<T>(string packagePath) where T : UObject
        {
            return LoadObjectAsync<T>(packagePath).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Attempts to retrieve a package index export of type T.
        /// </summary>
        /// <typeparam name="T">The type of UObject to retrieve.</typeparam>
        /// <param name="packageIndex">The package index reference.</param>
        /// <param name="export">The exported object if found.</param>
        /// <returns>True if the export was successfully retrieved; otherwise, false.</returns>
        public static bool TryGetPackageIndexExport<T>(FPackageIndex? packageIndex, out T export) where T : UObject
        {
            return packageIndex!.TryLoad(out export);
        }
    }
}
