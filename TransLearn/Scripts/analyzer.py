
import sys
import json
import spacy
import warnings             
warnings.filterwarnings("ignore")
# This script performs linguistic analysis on text piped from stdin.
# It identifies key words/phrases and good example sentences.
# The result is printed to stdout as a JSON string.

def is_good_context_sentence(sent):
    """
    주어와 동사가 있는지 확인하는 함수
    """
    has_subject = any(tok.dep_ == 'nsubj' for tok in sent)
    has_root_verb = any(tok.dep_ == 'ROOT' and tok.pos_ == 'VERB' for tok in sent)
    return has_subject and has_root_verb  # <--- 이 조건 때문에 탈락하는 겁니다!

def analyze_text(text, nlp):
    """
    Processes text using spaCy to extract learnable entries.
    """
    doc = nlp(text)
    learnable_entries = {}

    for sent in doc.sents:
        if not is_good_context_sentence(sent):
            continue

        for token in sent:
            # We are interested in nouns, proper nouns, verbs, and adjectives.
            # We skip stop words (common words like 'the', 'a', 'is') and punctuation.
            if token.pos_ in ['NOUN', 'PROPN', 'VERB', 'ADJ'] and not token.is_stop and not token.is_punct:
                # Use lemma (base form of the word) as the key.
                lemma = token.lemma_.lower()
                
                # If we haven't seen this word before, or the current sentence is longer
                # (potentially better context), we update the entry.
                if lemma not in learnable_entries or len(sent.text) > len(learnable_entries[lemma]['ContextSentence']):
                    learnable_entries[lemma] = {
                        "WordOrPhrase": lemma,
                        "ContextSentence": sent.text.strip()
                    }

    # Convert the dictionary to a list for JSON output.
    return list(learnable_entries.values())

def main():
    try:
        # Load the small English spaCy model.
        # Ensure you have run "python -m spacy download en_core_web_sm"
        nlp = spacy.load("en_core_web_sm")
    except OSError:
        sys.stderr.write("spaCy model 'en_core_web_sm' not found. Please run 'python -m spacy download en_core_web_sm'\n")
        sys.exit(1)
        
    # Read the entire input from stdin (piped from C#).
    input_text = sys.stdin.read()
    
    if not input_text:
        print(json.dumps([]))
        return

    # Analyze the text and get the results.
    results = analyze_text(input_text, nlp)
    
    # Print the results as a JSON string to stdout.
    print(json.dumps(results, indent=2))

if __name__ == "__main__":
    main()
