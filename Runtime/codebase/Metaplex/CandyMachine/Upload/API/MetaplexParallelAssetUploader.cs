using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public abstract class MetaplexParallelAssetUploader: MetaplexAssetUploader
{
    public abstract Task UploadAsset();

    public abstract Task Prepare();

    public async Task Upload() {
        return;
        // Default implementation of MetaplexAssetUploader methods for all Parallel Uploaders.
    }

}
