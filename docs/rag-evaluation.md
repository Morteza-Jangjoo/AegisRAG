# RAG Evaluation

## Overview

AegisRAG includes a reproducible evaluation process for measuring the quality of its retrieval pipeline.

The evaluation focuses on two important retrieval parameters:

* Chunk size and overlap
* Minimum cosine similarity threshold

The goal is to find a configuration that provides a good balance between retrieving relevant information and filtering irrelevant context.

---

## Evaluation Dataset

The evaluation uses a single one-page text document:

```text
TestFile.txt
```

The document contains information about STIHL and the German economy.

The benchmark contains **22 questions**:

* **16 relevant questions** whose answers are contained in the document
* **6 irrelevant questions** whose answers are not contained in the document

This allows the evaluation to measure both successful retrieval and false-positive retrieval.

### Retrieval Configuration

```text
TopK = 5
```

For every question, the system retrieves at most five chunks.

---

## Embedding Configuration

The evaluation uses Ollama with:

```text
Model      = nomic-embed-text
Dimensions = 768
```

The same embedding model is used for:

1. Indexing document chunks
2. Generating query embeddings

This ensures that document and query vectors exist in the same embedding space.

---

## Evaluation Metrics

The evaluation reports the following metrics.

### Hit@5

The percentage of relevant questions for which at least one relevant chunk was retrieved in the top five results.

### Precision@5

The percentage of retrieved results that are considered relevant.

### Recall@5

The percentage of relevant questions for which the expected relevant information was successfully retrieved.

### F1

The harmonic mean of Precision and Recall:

```text
F1 = 2 × Precision × Recall / (Precision + Recall)
```

F1 is used as the primary metric for selecting the final configuration because it balances retrieval precision and recall.

### False Positive Rate

The percentage of irrelevant questions for which the system retrieves a chunk considered relevant according to the evaluation benchmark.

A lower value is preferred.

---

# Chunking Evaluation

Three chunking configurations were evaluated:

| Configuration |     Hit@5 | Precision |    Recall |        F1 |      FPR |
| ------------- | --------: | --------: | --------: | --------: | -------: |
| 1000 / 200    |     75.0% |     37.2% |     75.0% |     49.7% |     0.0% |
| **700 / 150** | **81.2%** | **38.8%** | **81.2%** | **52.5%** | **0.0%** |
| 500 / 100     |     93.8% |     34.7% |     93.8% |     50.6% |     0.0% |

The `700 / 150` configuration achieved the highest F1 score among the three configurations.

Although `500 / 100` achieved higher recall, its lower precision resulted in a lower overall F1 score.

Therefore:

```text
Selected Chunk Size = 700
Selected Overlap    = 150
```

---

# Similarity Threshold Evaluation

After selecting `700 / 150`, the similarity threshold was tuned.

The first evaluation covered thresholds from `0.55` to `0.65`.

A second evaluation focused on the higher range from `0.66` to `0.72`.

The final evaluation produced:

| Threshold |     Hit@5 | Precision |    Recall |        F1 |      FPR |
| --------: | --------: | --------: | --------: | --------: | -------: |
|      0.66 |     81.2% |     50.9% |     81.2% |     62.6% |     0.0% |
|      0.67 |     81.2% |     50.9% |     81.2% |     62.6% |     0.0% |
|      0.68 |     81.2% |     51.9% |     81.2% |     63.4% |     0.0% |
|  **0.69** | **75.0%** | **55.0%** | **75.0%** | **63.5%** | **0.0%** |
|      0.70 |     62.5% |     51.5% |     62.5% |     56.5% |     0.0% |
|      0.71 |     62.5% |     53.1% |     62.5% |     57.4% |     0.0% |
|      0.72 |     56.2% |     56.0% |     56.2% |     56.1% |     0.0% |

The highest F1 score was achieved at:

```text
Minimum Similarity = 0.69
```

At this threshold:

```text
Precision = 55.0%
Recall    = 75.0%
F1        = 63.5%
FPR       = 0.0%
```

The threshold was selected based on the highest F1 score while maintaining a zero false-positive rate in the current benchmark.

---

# Final Retrieval Configuration

Based on the evaluation results, the current retrieval configuration is:

```text
Embedding Model
nomic-embed-text

Embedding Dimensions
768

Chunk Size
700

Chunk Overlap
150

TopK
5

Minimum Similarity
0.69
```

This configuration is currently used as the baseline retrieval configuration for AegisRAG.

---

# Reproducibility

The evaluation is implemented as automated integration tests.

The evaluation process performs the following steps:

```text
1. Clear the evaluation database
        ↓
2. Extract TestFile.txt
        ↓
3. Chunk the document
        ↓
4. Generate embeddings using Ollama
        ↓
5. Store chunks and embeddings in PostgreSQL + pgvector
        ↓
6. Generate embeddings for the 22 evaluation questions
        ↓
7. Execute vector similarity searches
        ↓
8. Evaluate retrieved chunks
        ↓
9. Calculate Precision, Recall, F1 and FPR
        ↓
10. Compare configurations
```

The relevant evaluation components are located under:

```text
tests/AegisRAG.IntegrationTests/Evaluation/
```

Including:

```text
EvaluationDataSeeder.cs
RagEvaluationRunner.cs
ChunkingEvaluationRunner.cs
rag-evaluation.json
TestFile.txt
```

The evaluation can be executed with:

```bash
dotnet test tests/AegisRAG.IntegrationTests
```

Specific evaluation runners can also be executed using the corresponding test filter.

---

# Limitations

The current benchmark is intentionally small and should not be interpreted as a general measurement of RAG quality.

Current limitations include:

* The evaluation dataset contains only 22 questions.
* The benchmark uses a single source document.
* The document contains only one page of text.
* The evaluation focuses primarily on retrieval quality rather than end-to-end answer quality.
* Chunking is currently character-based.
* The benchmark does not yet measure answer faithfulness or hallucination rate.
* Results may vary slightly between runs because embeddings and retrieval are evaluated against a live local AI environment.

Therefore, the selected configuration should be considered the **current benchmark baseline** rather than a universally optimal RAG configuration.

---

# Future Evaluation Improvements

Future versions of the evaluation can expand the benchmark with:

* Multiple documents
* Multiple document types
* Larger question sets
* More difficult semantic questions
* Answer correctness evaluation
* Context relevance evaluation
* Faithfulness / hallucination evaluation
* Different embedding models
* Different chunking strategies
* Hybrid keyword + vector retrieval
* Reranking
* End-to-end RAG evaluation

The current evaluation provides a reproducible foundation for measuring these improvements.
