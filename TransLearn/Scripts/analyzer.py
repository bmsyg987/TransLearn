import sys
import json
import warnings
import spacy
import nltk
import re
from nltk.corpus import wordnet

# 1. 경고 및 에러 무시 설정
warnings.filterwarnings("ignore")

# 2. 인코딩 강제 설정 (한글 윈도우 충돌 방지 + 깨진 문자 무시)
sys.stdin.reconfigure(encoding='utf-8', errors='ignore')
sys.stdout.reconfigure(encoding='utf-8', errors='ignore')

# 3. NLTK 데이터 다운로드 (사전 기능)
try:
    nltk.data.find('corpora/wordnet.zip')
except LookupError:
    nltk.download('wordnet', quiet=True)
    nltk.download('omw-1.4', quiet=True)

def get_wordnet_pos(spacy_tag):
    """spaCy 품사를 WordNet 품사로 변환"""
    if spacy_tag.startswith('J'): return wordnet.ADJ
    elif spacy_tag.startswith('V'): return wordnet.VERB
    elif spacy_tag.startswith('N'): return wordnet.NOUN
    elif spacy_tag.startswith('R'): return wordnet.ADV
    else: return None

def get_word_definition(lemma, pos_tag):
    """단어의 뜻(Definition)만 깔끔하게 가져오기"""
    wn_pos = get_wordnet_pos(pos_tag)
    if not wn_pos: return None

    synsets = wordnet.synsets(lemma, pos=wn_pos)
    if not synsets: return None

    # 가장 대표적인 뜻 1개 가져오기
    definition = synsets[0].definition()
    return definition

def is_pure_english(word):
    """
    [핵심 필터] 
    한글, 숫자, 특수문자가 0.001%라도 섞이면 바로 False 반환
    오직 a-z, A-Z로만 된 단어만 통과
    """
    return re.match(r'^[a-zA-Z]+$', word) is not None

def analyze_text(text, nlp):
    # 입력된 텍스트에서 줄바꿈 제거 (한 줄로 만들기)
    cleaned_text = text.replace('\n', ' ').replace('\r', ' ')
    
    doc = nlp(cleaned_text)
    learnable_entries = {}

    for token in doc:
        # 1. 품사 필터 (명사, 동사, 형용사만)
        if token.pos_ not in ['NOUN', 'VERB', 'ADJ']:
            continue
        
        # 2. 불용어(is, the, a 등) 제외
        if token.is_stop or token.is_punct:
            continue
            
        lemma = token.lemma_.lower()

        # 3. [초강력 필터] 순수 영어가 아니면 절대 저장하지 않음 (한글 원천 봉쇄)
        if not is_pure_english(lemma):
            continue

        # 4. 길이가 너무 짧은 단어 제외 (1~2글자 오타 방지)
        if len(lemma) < 3:
            continue

        # 5. 뜻 검색
        definition = get_word_definition(lemma, token.tag_)
        
        # 뜻이 없는 단어(사전에 안 나오는 고유명사 등)는 저장 안 함
        if not definition:
            continue

        # 6. 결과 저장
        # 요청하신 대로 '예문' 대신 '뜻'만 저장합니다.
        if lemma not in learnable_entries:
            learnable_entries[lemma] = {
                "WordOrPhrase": lemma,
                # 화면의 '뜻 & 예문' 칸에 뜻만 굵게 표시
                "ContextSentence": f"💡 Mean: {definition}", 
                "Frequency": 1,
                "Difficulty": 0.0
            }
        else:
            # 이미 있는 단어면 빈도수만 증가
            learnable_entries[lemma]["Frequency"] += 1

    return list(learnable_entries.values())

def main():
    try:
        # 영어 모델 로드
        nlp = spacy.load("en_core_web_sm")
    except Exception:
        # 모델 로드 실패 시 빈 리스트 반환
        print(json.dumps([]))
        return

    # C#에서 보내준 텍스트 읽기
    input_text = sys.stdin.read()
    
    if not input_text or not input_text.strip():
        print(json.dumps([]))
        return

    # 분석 시작
    results = analyze_text(input_text, nlp)
    
    # 결과 출력 (JSON)
    print(json.dumps(results, indent=2, ensure_ascii=False))

if __name__ == "__main__":
    main()