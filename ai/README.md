# AI training notes

## Param history
__2025-08-27__:
```json
{
  "classifier__C": 50,
  "classifier__max_iter": 500,
  "classifier__tol": 0.0001,
  "tfidf__ngram_range": "(1, 1)",
  "tfidf__norm": "None",
  "tfidf__sublinear_tf": false,
  "tfidf__use_idf": true
}
```
```
Best CV log-loss: -1.4300 (negated; closer to 0 is better)
Model evaluations:
CV(5) Accuracy:    0.4553 +/- 0.0236
CV(5) Log Loss:    2.1958 +/- 0.0545
CV(5) ROC AUC Train:   0.9716 +/- 0.0005
CV(5) ROC AUC Test:     0.9486 +/- 0.0027
CV(5) ROC AUC Train/Test delta:    0.023033
CV(5) NCDG(10) Train:    0.7343 +/- 0.0026
CV(5) NCDG(10) Test:    0.6594 +/- 0.0188
CV(5) NCDG Train/Test delta: 0.0750
Holdout NCDG(10) 0.6986
Holdout vs CV NCDG(10) 0.0392
Holdout Accuracy: 0.4964
Holdout Log Loss: 1.3321
Holdout vs CV Accuracy delta: 0.0410
```