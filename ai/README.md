# AI training notes

## 2025-08-30
Logistic Regression (LR) shows the best performance comparing to SVMs.
1. NDCG is 3-4 points higher with less train/test deltas (~0.70)
1. LR ONNX model size is orders of magnitude smaller (~50KB vs 10-30MBs)
1. Both models ranked top-5 plates about the same for sanity check queries, but LR produced much better confidence scores for top-1s.