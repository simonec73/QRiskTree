using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QRiskTree.Engine.ImportExport
{
    /// <summary>
    /// Import files into a risk model.
    /// </summary>
    public interface IImporter
    {
        /// <summary>
        /// Description of the file type that this importer can handle. 
        /// This is used for display purposes in the UI when allowing users to select a file to import.
        /// </summary>
        public string FileDescription { get; }
        
        /// <summary>
        /// The file extension that this importer can handle.
        /// This is used to filter files in the UI when allowing users to select a file to import.
        /// </summary>
        public string FileExtension { get; }

        /// <summary>
        /// Try to import a file into the given risk model. 
        /// The method should return true if the import was successful, and false otherwise.
        /// </summary>
        /// <param name="filePath">The path to the file to be imported.</param>
        /// <param name="model">The risk model into which the file should be imported.</param>
        /// <returns>True if the import was successful, false otherwise.</returns>
        /// <remarks>If the method returns false, the risk model is unchanged. 
        /// In case an exception is raised, the risk model may be partially modified.</remarks>
        bool TryImportingIntoRiskModel<R, M>(string filePath, ref IRiskModel<R, M> model) where R : INamedObject where M : INamedObject;
    }
}
