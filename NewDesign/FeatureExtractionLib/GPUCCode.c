kernel void dfprocess(
        global read_only short* meta_tree, 
        global read_only int* trees, 
        global read_only short* x,
        global write_only float* y)
{
        int index= get_global_id(0);    
        int offs, k, idx;
        short i;
        float v;
        v = (float)1 / (float)meta_tree[1];
        for (i=0; i< meta_tree[0]; i++)
            y[i] = 0;
        for (i=0; i< meta_tree[1]; i++){
            k = offs +1;
            while (1){
                if (trees[k] == -1)
                {
                   idx = trees[k+1];
                   y[idx]++;
                   break;
                }
                if (x[trees[k]] < trees[k+1] )
                    k+=3;
                else
                    k = offs + trees[k+2];
            }
            offs = offs + trees[offs];
        }
        for (i=0; i< meta_tree[0]; i++)
            y[i] = v* y[i];
}  


int GetNewDepthIndex(int cur_index, int dx, int dy)
{
    int cx = (cur_index % 640) + dx;
    int cy = (cur_index / 640) + dy;
    if (cx>=0 && cx< 640 && cy>=0 && cy< 480)
        return (cy*640 + cx);
    else 
        return -1;
} 
kernel void Predict(
    global read_only short* meta_tree,     
    global read_only int* trees, 
    global read_only int* offset_list,
    global write_only float* y,    
    global read_only short* depth)
{
    int one_dim_index= get_global_id(0);    
    int feature_index = 0;
    int offset_list_index = feature_index*4;
    short u_depth = GetNewDepth( one_dim_index, offset_list[offset_list_index], offset_list[offset_list_index+1], depth);
    short v_depth = GetNewDepth( one_dim_index, offset_list[offset_list_index+2], offset_list[offset_list_index+3], depth);
    y[one_dim_index] = (float) (u_depth - v_depth);
}

/*
    int one_dim_index= get_global_id(0);    
    int feature_index = 0;
    int offset_list_index = feature_index*4;
    //int u_depth_index = GetNewDepthIndex(one_dim_index, offset_list[offset_list_index], offset_list[offset_list_index+1]) ;
    //int v_depth_index = GetNewDepthIndex(one_dim_index, offset_list[offset_list_index+2], offset_list[offset_list_index+3]);
    short u_depth = 0; //(u_depth_index == -1)? 10000 : depth[u_depth_index];
    short v_depth = 0; //(v_depth_index == -1)? 10000 : depth[v_depth_index];
    //y[one_dim_index] = (float) (u_depth - v_depth);
*/


int GetNewDepthIndex(int cur_index, int dx, int dy)
{
    int cx = (cur_index % 640) + dx;
    int cy = (cur_index / 640) + dy;
    if (cx>=0 && cx< 640 && cy>=0 && cy< 480)
        return (cy*640 + cx);
    else 
        return -1;
} 
kernel void Predict(
    global read_only short* meta_tree,     
    global read_only int* trees, 
    global read_only int* offset_list,
    global write_only float* y,    
    global read_only short* depth)
{
    int one_dim_index= get_global_id(0);    
    int feature_index = 0;
    int offset_list_index = feature_index*4;
    int u_depth_index = GetNewDepthIndex(one_dim_index, offset_list[offset_list_index], offset_list[offset_list_index+1]) ;
    int v_depth_index = GetNewDepthIndex(one_dim_index, offset_list[offset_list_index+2], offset_list[offset_list_index+3]);
    short u_depth = (u_depth_index == -1)? 10000 : depth[u_depth_index];
    short v_depth = (v_depth_index == -1)? 10000 : depth[v_depth_index];
    y[one_dim_index] = (float) (u_depth - v_depth);
}

------------------------------

// return the transformed point's one dimension index, if it's out-of-bound return -1.
int GetNewDepthIndex(int cur_index, int dx, int dy)
{
    int cx = (cur_index % 640) + dx;
    int cy = (cur_index / 640) + dy;
    if (cx>=0 && cx< 640 && cy>=0 && cy< 480)
        return (cy*640 + cx);
    else 
        return -1;
} 
kernel void Predict(
    global read_only short* meta_tree,     
    global read_only int* trees, 
    global read_only int* offset_list,
    global write_only float* y,    
    global read_only short* depth)
{
    int index= get_global_id(0), y_index =index* meta_tree[0];    
    int u_depth_index, v_depth_index, offs = 0, k, idx, offset_list_index;    
    short u_depth, v_depth, i;    
    float v;    
    v = (float)1 / (float)meta_tree[1];
    for (i=0; i< meta_tree[0]; i++)
        y[y_index + i] = 0;
    for (i=0; i< meta_tree[1]; i++){
        k = offs +1;
        while (1){
            if (trees[k] == -1)
            {
                idx = trees[k+1];
                y[y_index + idx]++;
                break;
            }
            // get the feature value
            offset_list_index = trees[k]*4;
            u_depth_index = GetNewDepthIndex(index, offset_list[offset_list_index], offset_list[offset_list_index+1]) ;
            v_depth_index = GetNewDepthIndex(index, offset_list[offset_list_index+2], offset_list[offset_list_index+3]);
            u_depth = (u_depth_index == -1)? 10000 : depth[u_depth_index];
            v_depth = (v_depth_index == -1)? 10000 : depth[v_depth_index];
            if (u_depth - v_depth < trees[k+1] )
                k+=3;
            else
                k = offs + trees[k+2];
        }
        offs = offs + trees[offs];
    }
    for (i=0; i< meta_tree[0]; i++)
        y[y_index + i] = v* y[y_index + i];
}