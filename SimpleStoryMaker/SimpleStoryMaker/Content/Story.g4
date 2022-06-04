grammar Story;

program: player globalCode* story EOF;

player: 'PLAYER''{'attribute*'}';

attribute: IDENTIFIER ':' expression';';

expression
    : literal                           #literalExpression
    | function                          #functionExpression
    | 'PLAYER.'IDENTIFIER               #playerCallExpression
    | IDENTIFIER                        #variableCallExpression
    | '(' expression ')'                #paranthesizedExpression
    | '-' expression                    #negativeExpression
    | '!' expression                    #notExpression
    | expression MULTOP expression      #multiplicativeExpression
    | expression ADDOP expression       #additiveExpression
    | expression COMPAREOP expression   #comparisonExpression
    | expression BOOLOP expression      #booleanExpression
    ;
globalCode: code | globalDeclaration;
localCode: code | localDeclaration;
code: assignment | ifBlock | whileBlock | playerAttributeAssignment ;

globalDeclaration: 'var 'assignment;
localDeclaration: 'local 'assignment;

assignment: IDENTIFIER '=' expression ';';
playerAttributeAssignment: 'PLAYER.'IDENTIFIER '=' expression ';';

ifBlock: if elif* else?;

if: 'if ' expression '{' localScope '}';
elif: 'elif ' expression '{' localScope '}';
else: 'else ' '{' localScope '}';

whileBlock: 'while ' expression '{' localScope '}';

function: rand_func;

rand_func: 'RAND''('expression','expression')';

story: start scene* end;

start: SCENE START'{'sceneScope'}';
end: SCENE END'{'endSceneScope'}';
scene: SCENE IDENTIFIER'{'sceneScope'}';

sceneScope : localScope write (choices | goTo);
endSceneScope: localScope write;
localScope: localCode*;

write: 'write'':'expression';';

choices:'choices''{'choice+'}';

choice: choiceCondition? IDENTIFIER'{'choiceScope'}';
choiceScope: localCode* choiceText (goTo | write);

choiceCondition: 'if ' expression;
choiceText: 'text'':'expression';';
goTo: 'goto'':'(IDENTIFIER | START | END)';';

literal: number | STRING | BOOL;

number: INT | INT'.'INT;

MULTOP: '*' | '/';
ADDOP: '+' | '-';
COMPAREOP: '==' | '!=' | '>' | '<' | '>=' | '<=';
BOOLOP: 'and' | 'or';

INT: [0-9]+;
STRING: ('"' ~'"'* '"') | ('\'' ~'\''* '\'');
BOOL: 'true' | 'false';

WS: [ \t\r\n]+ -> skip;
START: 'START';
END: 'END';
SCENE: 'SCENE ';
IDENTIFIER: [a-zA-Z_][a-zA-Z0-9_]*;