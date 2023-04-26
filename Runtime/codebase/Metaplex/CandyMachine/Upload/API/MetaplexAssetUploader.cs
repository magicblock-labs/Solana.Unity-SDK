using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public interface MetaplexAssetUploader
{
    public Task Prepare();

    public Task Upload();
}
